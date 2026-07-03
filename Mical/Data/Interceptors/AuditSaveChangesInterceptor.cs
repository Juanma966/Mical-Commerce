using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using Mical.Entities;
using Mical.Entities.Common;
using Mical.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Mical.Data.Interceptors;

/// <summary>
/// Escribe un <see cref="AuditLog"/> por cada alta/cambio/baja de las entidades
/// auditadas (Product, Category, Order), pero SOLO cuando la acción la ejecuta
/// un administrador. Así se registran las operaciones del panel admin sin ensuciar
/// cada Service, y se ignoran las acciones públicas (checkout, registro, etc.).
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> Audited = new() { "Product", "Category", "Order" };
    private static readonly HashSet<string> IgnoredProps = new() { "UpdatedAt", "CreatedAt", "xmin" };

    private readonly IHttpContextAccessor _http;

    // Estado entre SavingChanges y SavedChanges, por contexto (seguro ante concurrencia).
    private readonly ConcurrentDictionary<DbContext, List<PendingAudit>> _pending = new();

    public AuditSaveChangesInterceptor(IHttpContextAccessor http)
    {
        _http = http;
    }

    private sealed class PendingAudit
    {
        public required EntityEntry Entry { get; init; }
        public required string Action { get; init; }
        public required string EntityName { get; init; }
        public string? Details { get; init; }
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Prepare(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Prepare(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await FinalizeAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        FinalizeAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private void Prepare(DbContext? context)
    {
        if (context is null) return;

        var user = _http.HttpContext?.User;
        if (user?.IsInRole(Roles.Administrador) != true) return; // solo acciones de admin

        var list = new List<PendingAudit>();
        foreach (var entry in context.ChangeTracker.Entries())
        {
            var name = entry.Entity.GetType().Name;
            if (!Audited.Contains(name)) continue;

            var (action, details) = entry.State switch
            {
                EntityState.Added => ("Created", Label(entry)),
                EntityState.Deleted => ("Deleted", Label(entry)),
                EntityState.Modified => IsSoftDelete(entry)
                    ? ("Deleted", Label(entry))
                    : ("Updated", ChangedSummary(entry)),
                _ => (string.Empty, (string?)null)
            };

            if (action.Length == 0) continue;
            list.Add(new PendingAudit { Entry = entry, Action = action, EntityName = name, Details = details });
        }

        if (list.Count > 0)
            _pending[context] = list;
    }

    private async Task FinalizeAsync(DbContext? context, CancellationToken ct)
    {
        if (context is null || !_pending.TryRemove(context, out var list))
            return;

        var user = _http.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = user?.Identity?.Name;
        var now = DateTime.UtcNow;

        foreach (var p in list)
        {
            context.Add(new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = p.Action,
                EntityName = p.EntityName,
                EntityId = KeyOf(p.Entry),
                Timestamp = now,
                Details = p.Details
            });
        }

        // Segundo guardado: AuditLog no está auditado, así que no hay recursión.
        await context.SaveChangesAsync(ct);
    }

    private static bool IsSoftDelete(EntityEntry entry)
    {
        if (entry.Entity is not ISoftDeletable) return false;
        var prop = entry.Property(nameof(ISoftDeletable.IsDeleted));
        return prop.CurrentValue is true && prop.OriginalValue is false;
    }

    private static string? KeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return null;
        var vals = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString());
        return string.Join(",", vals);
    }

    /// <summary>Etiqueta legible: nombre/número de la entidad si lo tiene.</summary>
    private static string? Label(EntityEntry entry)
    {
        foreach (var candidate in new[] { "Name", "OrderNumber" })
        {
            var prop = entry.Metadata.FindProperty(candidate);
            if (prop is not null)
                return entry.Property(candidate).CurrentValue?.ToString();
        }
        return null;
    }

    /// <summary>Resumen de propiedades cambiadas: "Prop: viejo → nuevo".</summary>
    private static string? ChangedSummary(EntityEntry entry)
    {
        var sb = new StringBuilder();
        foreach (var prop in entry.Properties)
        {
            if (!prop.IsModified) continue;
            if (IgnoredProps.Contains(prop.Metadata.Name)) continue;

            if (sb.Length > 0) sb.Append("; ");
            sb.Append(prop.Metadata.Name).Append(": ")
              .Append(prop.OriginalValue).Append(" → ").Append(prop.CurrentValue);
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }
}

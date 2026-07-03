namespace Mical.Entities;

/// <summary>
/// Registro append-only de una acción de escritura del panel admin. Lo escribe
/// el interceptor de SaveChanges (ver Data/Interceptors). Solo acciones de admin.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Id del usuario que hizo la acción (nullable para acciones del sistema).</summary>
    public string? UserId { get; set; }

    /// <summary>Nombre/email legible del usuario (snapshot).</summary>
    public string? UserName { get; set; }

    /// <summary>Created / Updated / Deleted.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Entidad afectada (ej. "Product").</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Clave de la entidad afectada.</summary>
    public string? EntityId { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>Resumen legible del cambio.</summary>
    public string? Details { get; set; }
}

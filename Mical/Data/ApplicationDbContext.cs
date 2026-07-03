using Mical.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Mical.Data;

/// <summary>
/// Contexto de EF Core de la aplicación. Hereda de IdentityDbContext para
/// incorporar las tablas estándar de ASP.NET Identity (AspNetUsers, AspNetRoles,
/// etc.) usando <see cref="ApplicationUser"/> como usuario.
/// A partir de la Fase 2 se irán agregando los DbSet del dominio y sus configuraciones.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Carga automática de todas las configuraciones IEntityTypeConfiguration<T>
        // ubicadas en Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Secuencia para el correlativo del SKU de productos (concurrencia segura).
        modelBuilder.HasSequence<long>("product_sku_seq").StartsAt(1).IncrementsBy(1);

        // Secuencia para el número de pedido (ORD-2026-000123).
        modelBuilder.HasSequence<long>("order_number_seq").StartsAt(1).IncrementsBy(1);

        // Extensión trigram para acelerar la búsqueda ILIKE por nombre (Fase 4.2).
        modelBuilder.HasPostgresExtension("pg_trgm");
    }
}

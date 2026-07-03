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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Carga automática de todas las configuraciones IEntityTypeConfiguration<T>
        // ubicadas en Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

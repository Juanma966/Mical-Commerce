using Microsoft.EntityFrameworkCore;

namespace Mical.Data;

/// <summary>
/// Contexto de EF Core de la aplicación. Por ahora vacío (Fase 0.3).
/// En la Fase 1 pasará a heredar de IdentityDbContext&lt;ApplicationUser&gt;,
/// y a partir de la Fase 2 se irán agregando los DbSet y sus configuraciones.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Carga automática de todas las configuraciones IEntityTypeConfiguration<T>
        // ubicadas en Data/Configurations (todavía no hay ninguna).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

using Mical.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mical.Data.Configurations;

/// <summary>Mapeo Fluent API de <see cref="Category"/>.</summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.ImagePath)
            .HasMaxLength(255);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.IsDeleted)
            .HasDefaultValue(false);

        // Nombre único (la unicidad se evalúa sobre las filas no borradas).
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // Borrado lógico: filtro global para excluir las categorías eliminadas.
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

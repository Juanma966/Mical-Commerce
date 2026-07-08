using Mical.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mical.Data.Configurations;

/// <summary>Mapeo Fluent API de <see cref="Promotion"/>.</summary>
public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(p => p.Subtitle)
            .HasMaxLength(200);

        builder.Property(p => p.ImagePath)
            .HasMaxLength(255);

        builder.Property(p => p.LinkUrl)
            .HasMaxLength(300);

        builder.Property(p => p.LinkText)
            .HasMaxLength(50);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        // Consulta típica de la home: activas, ordenadas.
        builder.HasIndex(p => new { p.IsActive, p.DisplayOrder });

        // Borrado lógico.
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

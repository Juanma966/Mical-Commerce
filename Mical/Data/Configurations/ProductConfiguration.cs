using Mical.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mical.Data.Configurations;

/// <summary>Mapeo Fluent API de <see cref="Product"/>.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        // Description sin longitud máxima → columna text.

        builder.Property(p => p.Price)
            .HasPrecision(12, 2);

        builder.Property(p => p.SalePrice)
            .HasPrecision(12, 2);

        builder.Property(p => p.ImagePath)
            .HasMaxLength(255);

        builder.Property(p => p.MinStock)
            .HasDefaultValue(0);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        // SKU único a nivel global (la secuencia garantiza que no se reutilice).
        builder.HasIndex(p => p.Sku).IsUnique();

        // Relación con Categoría: Restrict para no romper el historial de pedidos.
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.CategoryId);

        // Índice del filtro de catálogo (productos visibles).
        builder.HasIndex(p => new { p.IsDeleted, p.IsActive });

        // Índice GIN trigram para búsqueda parcial por nombre (ILIKE '%term%').
        builder.HasIndex(p => p.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Borrado lógico.
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Concurrencia optimista para el stock (anti-sobreventa en el checkout).
        // Usa la columna de sistema xmin de PostgreSQL como token: no agrega columna
        // propia ni genera DDL. La API está marcada obsoleta pero es la forma correcta
        // para xmin en Npgsql; el equivalente manual haría que EF intente crear la columna.
#pragma warning disable CS0618 // UseXminAsConcurrencyToken obsoleto
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

        // Propiedades calculadas: no se persisten.
        builder.Ignore(p => p.EffectivePrice);
        builder.Ignore(p => p.IsOnSale);
        builder.Ignore(p => p.IsOutOfStock);
    }
}

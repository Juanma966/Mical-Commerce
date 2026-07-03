using Mical.Entities.Common;

namespace Mical.Entities;

/// <summary>
/// Producto del catálogo. Pertenece a una <see cref="Category"/>. Usa borrado
/// lógico. El <see cref="Sku"/> se autogenera al crear y no es editable (Fase 3.2).
/// </summary>
public class Product : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>Código único, autogenerado y no editable (ej. PRD-2026-000123).</summary>
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Precio de lista (mayor a 0).</summary>
    public decimal Price { get; set; }

    /// <summary>Precio de oferta opcional (menor a <see cref="Price"/>).</summary>
    public decimal? SalePrice { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Ruta relativa de la imagen en wwwroot/uploads/products.</summary>
    public string? ImagePath { get; set; }

    public int Stock { get; set; }
    public int MinStock { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Precio efectivo de venta: la oferta si existe, si no el de lista.</summary>
    public decimal EffectivePrice => SalePrice ?? Price;

    public bool IsOnSale => SalePrice.HasValue && SalePrice.Value < Price;

    public bool IsOutOfStock => Stock <= 0;
}

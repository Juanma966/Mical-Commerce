namespace Mical.ViewModels;

/// <summary>Producto en la grilla del catálogo.</summary>
public class ProductCardVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public decimal Price { get; set; }
    public decimal EffectivePrice { get; set; }

    /// <summary>Precio mayorista. Null solo en productos cargados antes del campo.</summary>
    public decimal? WholesalePrice { get; set; }

    public bool IsOnSale { get; set; }
    public bool IsOutOfStock { get; set; }
}

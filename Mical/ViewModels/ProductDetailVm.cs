namespace Mical.ViewModels;

/// <summary>Detalle de producto en la página pública.</summary>
public class ProductDetailVm
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public decimal Price { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsOnSale { get; set; }
    public int Stock { get; set; }
    public bool IsOutOfStock { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

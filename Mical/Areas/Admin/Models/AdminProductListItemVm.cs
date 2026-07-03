namespace Mical.Areas.Admin.Models;

/// <summary>Fila del listado de productos en el panel admin.</summary>
public class AdminProductListItemVm
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public bool IsActive { get; set; }
    public string? ImagePath { get; set; }

    public decimal EffectivePrice => SalePrice ?? Price;
    public bool IsLowStock => Stock <= MinStock;
}

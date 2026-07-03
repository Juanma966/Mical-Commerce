using Mical.Models;

namespace Mical.ViewModels;

/// <summary>Categoría disponible como filtro en el catálogo.</summary>
public class CategoryFilterVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Datos de la página <c>/shop</c>: productos paginados + estado de filtros.</summary>
public class ShopIndexVm
{
    public PagedResult<ProductCardVm> Products { get; set; } = new();
    public IReadOnlyList<CategoryFilterVm> Categories { get; set; } = new List<CategoryFilterVm>();
    public int? SelectedCategoryId { get; set; }
    public string? Query { get; set; }
}

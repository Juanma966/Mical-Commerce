using Mical.ViewModels;

namespace Mical.Services.Interfaces;

/// <summary>
/// Consultas de solo lectura del catálogo público. Solo expone productos activos
/// de categorías activas.
/// </summary>
public interface ICatalogService
{
    Task<ShopIndexVm> GetShopAsync(int? categoryId, string? query, int page, int pageSize);

    Task<ProductDetailVm?> GetProductDetailAsync(int id);
}

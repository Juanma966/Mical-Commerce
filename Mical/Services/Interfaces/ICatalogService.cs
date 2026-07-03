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

    /// <summary>
    /// Re-valida un carrito del cliente contra la base: resuelve precios y stock
    /// actuales, recorta cantidades al stock y marca los productos no disponibles.
    /// Nunca confía en el precio del cliente.
    /// </summary>
    Task<CartVm> RehydrateCartAsync(IEnumerable<CartItemInput> items);
}

using Mical.ViewModels;

namespace Mical.Services.Interfaces;

/// <summary>
/// Consultas de solo lectura del catálogo público. Solo expone productos activos
/// de categorías activas.
/// </summary>
public interface ICatalogService
{
    Task<ShopIndexVm> GetShopAsync(int? categoryId, string? query, int page, int pageSize);

    /// <summary>
    /// Productos para la sección "destacados" de la home. Por ahora toma los más
    /// recientes activos; a futuro puede filtrar por un flag IsFeatured.
    /// </summary>
    Task<IReadOnlyList<ProductCardVm>> GetFeaturedAsync(int count);

    /// <summary>
    /// Sugerencias para el autocomplete del buscador (coincidencia por nombre).
    /// Devuelve vacío si la consulta es demasiado corta.
    /// </summary>
    Task<IReadOnlyList<ProductSuggestionVm>> SuggestAsync(string query, int limit);

    Task<ProductDetailVm?> GetProductDetailAsync(int id);

    /// <summary>Productos y categorías visibles para generar el sitemap.xml.</summary>
    Task<SitemapDataVm> GetSitemapDataAsync();

    /// <summary>
    /// Re-valida un carrito del cliente contra la base: resuelve precios y stock
    /// actuales, recorta cantidades al stock y marca los productos no disponibles.
    /// Nunca confía en el precio del cliente.
    /// </summary>
    Task<CartVm> RehydrateCartAsync(IEnumerable<CartItemInput> items);
}

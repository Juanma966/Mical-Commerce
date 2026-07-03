using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>Catálogo público: listado con filtro por categoría, búsqueda y paginación.</summary>
public class ShopController : Controller
{
    private const int PageSize = 12;

    private readonly ICatalogService _catalog;

    public ShopController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    // GET: /shop?categoria=&q=&page=
    public async Task<IActionResult> Index(
        [FromQuery(Name = "categoria")] int? categoria,
        [FromQuery(Name = "q")] string? q,
        [FromQuery(Name = "page")] int page = 1)
    {
        var model = await _catalog.GetShopAsync(categoria, q, page, PageSize);
        return View(model);
    }
}

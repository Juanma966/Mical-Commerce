using Mical.Services.Interfaces;
using Mical.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>
/// Carrito del lado del cliente. El estado vive en LocalStorage (solo id + cantidad);
/// este controlador solo renderiza la página y re-valida los ítems contra la base.
/// </summary>
public class CartController : Controller
{
    private readonly ICatalogService _catalog;

    public CartController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    // GET: /cart  → la vista se rellena con JS desde LocalStorage.
    public IActionResult Index() => View();

    // POST: /cart/rehydrate → recibe [{productId, quantity}] y devuelve el carrito
    // validado (precios y stock actuales). Solo lectura: sin efectos de lado, sin
    // datos privados → no requiere antiforgery.
    [HttpPost("/cart/rehydrate")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Rehydrate([FromBody] List<CartItemInput>? items)
    {
        var cart = await _catalog.RehydrateCartAsync(items ?? new List<CartItemInput>());
        return Json(cart);
    }
}

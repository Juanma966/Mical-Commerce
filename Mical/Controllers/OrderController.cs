using Mical.Entities;
using Mical.Services.Interfaces;
using Mical.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>Pedidos del usuario autenticado (detalle y, en 6.2, el historial).</summary>
[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orders;
    private readonly ICatalogService _catalog;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderController(IOrderService orders, ICatalogService catalog, UserManager<ApplicationUser> userManager)
    {
        _orders = orders;
        _catalog = catalog;
        _userManager = userManager;
    }

    // GET: /order  → "Mis pedidos"
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var orders = await _orders.GetHistoryForUserAsync(userId);
        return View(orders);
    }

    // GET: /order/details/5  (placed=true justo después de comprar → confirmación)
    public async Task<IActionResult> Details(int id, bool placed = false)
    {
        var userId = _userManager.GetUserId(User)!;
        var order = await _orders.GetForUserAsync(id, userId);
        if (order is null)
            return NotFound();

        ViewData["JustPlaced"] = placed;
        return View(order);
    }

    // GET: /order/reorder/5  → devuelve los ítems del pedido que siguen disponibles
    // (revalidados contra stock actual) para recargarlos en el carrito del cliente.
    public async Task<IActionResult> Reorder(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var order = await _orders.GetForUserAsync(id, userId);
        if (order is null)
            return NotFound();

        var requested = order.Items
            .Select(i => new CartItemInput { ProductId = i.ProductId, Quantity = i.Quantity })
            .ToList();

        var cart = await _catalog.RehydrateCartAsync(requested);

        var available = cart.Lines
            .Where(l => l.Available && l.Quantity > 0)
            .Select(l => new { productId = l.ProductId, quantity = l.Quantity })
            .ToList();

        return Json(new
        {
            items = available,
            // Hay ajustes si algún producto se quitó o se recortó por stock.
            adjusted = cart.HasIssues,
            total = order.Items.Count,
            availableCount = available.Count
        });
    }
}

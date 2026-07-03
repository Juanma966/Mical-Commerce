using Mical.Entities;
using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>Pedidos del usuario autenticado (detalle y, en 6.2, el historial).</summary>
[Authorize]
public class OrderController : Controller
{
    private readonly IOrderService _orders;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrderController(IOrderService orders, UserManager<ApplicationUser> userManager)
    {
        _orders = orders;
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
}

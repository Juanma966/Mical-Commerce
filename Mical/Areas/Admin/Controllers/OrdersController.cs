using Mical.Entities;
using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Areas.Admin.Controllers;

/// <summary>Gestión de pedidos en el panel admin. Protegido vía <see cref="AdminBaseController"/>.</summary>
public class OrdersController : AdminBaseController
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    // GET: /Admin/Orders
    public async Task<IActionResult> Index()
    {
        var items = await _orders.GetAllForAdminAsync();
        return View(items);
    }

    // GET: /Admin/Orders/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orders.GetForAdminAsync(id);
        if (order is null)
            return NotFound();

        return View(order);
    }

    // POST: /Admin/Orders/ChangeStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, OrderStatus newStatus)
    {
        var result = await _orders.UpdateStatusAsync(id, newStatus);
        TempData["StatusMessage"] = result.Succeeded
            ? $"Pedido actualizado a {newStatus}."
            : result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }
}

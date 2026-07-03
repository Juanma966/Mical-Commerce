using Mical.Entities;
using Mical.Services.Interfaces;
using Mical.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>
/// Confirmación de compra. Requiere autenticación: un usuario anónimo es
/// redirigido a login con returnUrl. El carrito viaja desde LocalStorage y el
/// servidor lo re-valida al crear el pedido.
/// </summary>
[Authorize]
public class CheckoutController : Controller
{
    private readonly IOrderService _orders;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(IOrderService orders, UserManager<ApplicationUser> userManager)
    {
        _orders = orders;
        _userManager = userManager;
    }

    // GET: /checkout
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var model = new CheckoutVm
        {
            ContactName = user?.FullName ?? string.Empty,
            ContactPhone = user?.PhoneNumber ?? string.Empty
        };
        return View(model);
    }

    // POST: /checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;
        var result = await _orders.CheckoutAsync(userId, model);

        if (result.Succeeded)
            return RedirectToAction("Details", "Order", new { id = result.Value, placed = true });

        ModelState.AddModelError(string.Empty, result.Error!);
        return View(model);
    }
}

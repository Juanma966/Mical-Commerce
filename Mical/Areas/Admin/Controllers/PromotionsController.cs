using Mical.Areas.Admin.Models;
using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Areas.Admin.Controllers;

/// <summary>CRUD de promociones (banners de la home). Protegido vía <see cref="AdminBaseController"/>.</summary>
public class PromotionsController : AdminBaseController
{
    private readonly IPromotionService _promotions;

    public PromotionsController(IPromotionService promotions)
    {
        _promotions = promotions;
    }

    // GET: /Admin/Promotions
    public async Task<IActionResult> Index()
    {
        var items = await _promotions.GetAllForAdminAsync();
        return View(items);
    }

    // GET: /Admin/Promotions/Create
    public IActionResult Create() => View(new PromotionFormVm());

    // POST: /Admin/Promotions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PromotionFormVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _promotions.CreateAsync(model);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Promoción creada.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        return View(model);
    }

    // GET: /Admin/Promotions/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _promotions.GetForEditAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    // POST: /Admin/Promotions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PromotionFormVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _promotions.UpdateAsync(model);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Promoción actualizada.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        return View(model);
    }

    // POST: /Admin/Promotions/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _promotions.ToggleActiveAsync(id);
        TempData["StatusMessage"] = result.Succeeded ? "Promoción actualizada." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/Promotions/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _promotions.GetByIdAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    // POST: /Admin/Promotions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _promotions.DeleteAsync(id);
        TempData["StatusMessage"] = result.Succeeded ? "Promoción eliminada." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}

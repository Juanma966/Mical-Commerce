using Mical.Areas.Admin.Models;
using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Areas.Admin.Controllers;

/// <summary>CRUD de categorías en el panel admin. Protegido vía <see cref="AdminBaseController"/>.</summary>
public class CategoriesController : AdminBaseController
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
    {
        _categories = categories;
    }

    // GET: /Admin/Categories
    public async Task<IActionResult> Index()
    {
        var items = await _categories.GetAllForAdminAsync();
        return View(items);
    }

    // GET: /Admin/Categories/Create
    public IActionResult Create() => View(new CategoryFormVm());

    // POST: /Admin/Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _categories.CreateAsync(model);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Categoría creada.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(nameof(model.Name), result.Error!);
        return View(model);
    }

    // GET: /Admin/Categories/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _categories.GetForEditAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    // POST: /Admin/Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _categories.UpdateAsync(model);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Categoría actualizada.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(nameof(model.Name), result.Error!);
        return View(model);
    }

    // GET: /Admin/Categories/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _categories.GetByIdAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    // POST: /Admin/Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _categories.DeleteAsync(id);
        TempData["StatusMessage"] = result.Succeeded ? "Categoría eliminada." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}

using Mical.Areas.Admin.Models;
using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Areas.Admin.Controllers;

/// <summary>CRUD de productos en el panel admin. Protegido vía <see cref="AdminBaseController"/>.</summary>
public class ProductsController : AdminBaseController
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    // GET: /Admin/Products
    public async Task<IActionResult> Index()
    {
        var items = await _products.GetAllForAdminAsync();
        return View(items);
    }

    // GET: /Admin/Products/Create
    public async Task<IActionResult> Create()
    {
        var model = new ProductFormVm();
        await PopulateCategoriesAsync(model);
        return View(model);
    }

    // POST: /Admin/Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormVm model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model);
            return View(model);
        }

        var result = await _products.CreateAsync(model);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Producto creado.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        await PopulateCategoriesAsync(model);
        return View(model);
    }

    // GET: /Admin/Products/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _products.GetForEditAsync(id);
        if (model is null)
            return NotFound();

        await PopulateCategoriesAsync(model);
        return View(model);
    }

    // POST: /Admin/Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormVm model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model);
            return View(model);
        }

        var result = await _products.UpdateAsync(model);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Producto actualizado.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        await PopulateCategoriesAsync(model);
        return View(model);
    }

    // GET: /Admin/Products/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _products.GetByIdAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    // POST: /Admin/Products/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _products.DeleteAsync(id);
        TempData["StatusMessage"] = result.Succeeded ? "Producto eliminado." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(ProductFormVm model)
    {
        model.CategoryOptions = await _products.GetCategoryOptionsAsync();
    }
}

using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>Detalle público de producto (<c>/product/{id}</c>).</summary>
public class ProductController : Controller
{
    private readonly ICatalogService _catalog;

    public ProductController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("/product/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _catalog.GetProductDetailAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }
}

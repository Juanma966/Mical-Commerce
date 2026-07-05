using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mical.Models;
using Mical.Services.Interfaces;

namespace Mical.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICatalogService _catalog;

    public HomeController(ILogger<HomeController> logger, ICatalogService catalog)
    {
        _logger = logger;
        _catalog = catalog;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _catalog.GetFeaturedAsync(8);
        return View(featured);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // La página de error se re-ejecuta con el método original (a veces POST) vía
    // UseStatusCodePagesWithReExecute; no debe exigir antiforgery.
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode ?? 0
        };

        (model.Title, model.Message) = statusCode switch
        {
            404 => ("Página no encontrada", "La página que buscás no existe o fue movida."),
            403 => ("Acceso denegado", "No tenés permisos para acceder a esta página."),
            429 => ("Demasiados intentos", "Hiciste muchas solicitudes en poco tiempo. Esperá un momento e intentá de nuevo."),
            _ => ("Algo salió mal", "Ocurrió un error inesperado. Intentá nuevamente en unos minutos.")
        };

        Response.StatusCode = statusCode ?? 500;
        return View(model);
    }
}

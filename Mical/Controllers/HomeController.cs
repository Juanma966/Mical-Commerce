using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mical.Models;

namespace Mical.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
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

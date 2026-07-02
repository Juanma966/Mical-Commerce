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
            _ => ("Algo salió mal", "Ocurrió un error inesperado. Intentá nuevamente en unos minutos.")
        };

        Response.StatusCode = statusCode ?? 500;
        return View(model);
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Mical.Areas.Admin.Controllers;

/// <summary>
/// Página de inicio del panel de administración. Por ahora solo confirma el
/// acceso autorizado; las métricas del dashboard llegan en la Fase 7.2.
/// </summary>
public class DashboardController : AdminBaseController
{
    public IActionResult Index() => View();
}

using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Areas.Admin.Controllers;

/// <summary>Página de inicio del panel de administración con métricas.</summary>
public class DashboardController : AdminBaseController
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _dashboard.GetAsync();
        return View(vm);
    }
}

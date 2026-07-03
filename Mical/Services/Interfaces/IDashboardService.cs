using Mical.Areas.Admin.Models;

namespace Mical.Services.Interfaces;

/// <summary>Métricas para el panel de administración.</summary>
public interface IDashboardService
{
    Task<DashboardVm> GetAsync();
}

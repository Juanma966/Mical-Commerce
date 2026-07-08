using Mical.Areas.Admin.Models;
using Mical.Models;
using Mical.ViewModels;

namespace Mical.Services.Interfaces;

/// <summary>Lógica de negocio de promociones (banners de la home).</summary>
public interface IPromotionService
{
    // --- Panel admin ---
    Task<IReadOnlyList<AdminPromotionListItemVm>> GetAllForAdminAsync();
    Task<PromotionFormVm?> GetForEditAsync(int id);
    Task<AdminPromotionListItemVm?> GetByIdAsync(int id);
    Task<OperationResult> CreateAsync(PromotionFormVm model);
    Task<OperationResult> UpdateAsync(PromotionFormVm model);
    Task<OperationResult> DeleteAsync(int id);
    Task<OperationResult> ToggleActiveAsync(int id);

    // --- Público (home) ---
    /// <summary>Promociones activas y vigentes hoy, ordenadas para mostrar en la home.</summary>
    Task<IReadOnlyList<PromotionBannerVm>> GetActiveBannersAsync();
}

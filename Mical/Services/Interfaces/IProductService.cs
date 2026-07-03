using Mical.Areas.Admin.Models;
using Mical.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mical.Services.Interfaces;

/// <summary>Lógica de negocio de productos (panel admin).</summary>
public interface IProductService
{
    Task<IReadOnlyList<AdminProductListItemVm>> GetAllForAdminAsync();

    /// <summary>Formulario de edición (con SKU e imagen actual), o null si no existe.</summary>
    Task<ProductFormVm?> GetForEditAsync(int id);

    /// <summary>Fila para la confirmación de borrado, o null si no existe.</summary>
    Task<AdminProductListItemVm?> GetByIdAsync(int id);

    Task<OperationResult> CreateAsync(ProductFormVm model);
    Task<OperationResult> UpdateAsync(ProductFormVm model);
    Task<OperationResult> DeleteAsync(int id);

    /// <summary>Opciones de categorías para el desplegable del formulario.</summary>
    Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync();
}

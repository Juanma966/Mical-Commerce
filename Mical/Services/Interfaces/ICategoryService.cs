using Mical.Areas.Admin.Models;
using Mical.Entities;
using Mical.Models;

namespace Mical.Services.Interfaces;

/// <summary>Lógica de negocio de categorías.</summary>
public interface ICategoryService
{
    Task<IReadOnlyList<AdminCategoryListItemVm>> GetAllForAdminAsync();

    /// <summary>Devuelve el formulario de edición de una categoría, o null si no existe.</summary>
    Task<CategoryFormVm?> GetForEditAsync(int id);

    /// <summary>Datos mínimos para la confirmación de borrado, o null si no existe.</summary>
    Task<AdminCategoryListItemVm?> GetByIdAsync(int id);

    Task<OperationResult> CreateAsync(CategoryFormVm model);
    Task<OperationResult> UpdateAsync(CategoryFormVm model);
    Task<OperationResult> DeleteAsync(int id);
}

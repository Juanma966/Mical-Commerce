using System.ComponentModel.DataAnnotations;

namespace Mical.Areas.Admin.Models;

/// <summary>
/// Formulario de alta/edición de categoría en el panel admin. No expone la
/// entidad (evita over-posting); el mapeo VM↔entidad vive en el Service.
/// </summary>
public class CategoryFormVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Display(Name = "Activa")]
    public bool IsActive { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;

namespace Mical.Areas.Admin.Models;

/// <summary>Formulario de alta/edición de una promoción (banner de la home).</summary>
public class PromotionFormVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(120, ErrorMessage = "Máximo 120 caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    [Display(Name = "Subtítulo")]
    public string? Subtitle { get; set; }

    [StringLength(300, ErrorMessage = "Máximo 300 caracteres.")]
    [Url(ErrorMessage = "Ingresá una URL válida (ej. https://…).")]
    [Display(Name = "Enlace (URL)")]
    public string? LinkUrl { get; set; }

    [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
    [Display(Name = "Texto del botón")]
    public string? LinkText { get; set; }

    [Display(Name = "Orden")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Desde")]
    [DataType(DataType.Date)]
    public DateOnly? StartsAt { get; set; }

    [Display(Name = "Hasta")]
    [DataType(DataType.Date)]
    public DateOnly? EndsAt { get; set; }

    [Display(Name = "Activa")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Imagen")]
    public IFormFile? Image { get; set; }

    /// <summary>Ruta de la imagen actual (para mostrarla en edición).</summary>
    public string? ExistingImagePath { get; set; }
}

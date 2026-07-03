using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mical.Areas.Admin.Models;

/// <summary>
/// Formulario de alta/edición de producto. Las reglas de validación se aplican
/// con FluentValidation (ver ProductFormVmValidator); acá solo etiquetas y tipos.
/// El SKU no es editable: se muestra solo lectura en edición.
/// </summary>
public class ProductFormVm
{
    public int Id { get; set; }

    /// <summary>Solo lectura: autogenerado. Vacío al crear.</summary>
    [Display(Name = "SKU")]
    public string? Sku { get; set; }

    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Display(Name = "Precio")]
    public decimal Price { get; set; }

    [Display(Name = "Precio de oferta")]
    public decimal? SalePrice { get; set; }

    [Display(Name = "Categoría")]
    public int CategoryId { get; set; }

    [Display(Name = "Stock")]
    public int Stock { get; set; }

    [Display(Name = "Stock mínimo")]
    public int MinStock { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Imagen")]
    public IFormFile? Image { get; set; }

    /// <summary>Ruta de la imagen actual (para mostrarla en edición). No editable por el usuario.</summary>
    public string? ExistingImagePath { get; set; }

    /// <summary>Opciones del desplegable de categorías (las completa el controlador).</summary>
    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
}

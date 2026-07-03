using System.ComponentModel.DataAnnotations;

namespace Mical.ViewModels;

/// <summary>Datos editables del perfil del usuario autenticado.</summary>
public class ProfileVm
{
    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [Display(Name = "Nombre y apellido")]
    [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no es válido.")]
    [Display(Name = "Teléfono")]
    public string? PhoneNumber { get; set; }

    // Solo lectura: el email es el identificador de la cuenta (su cambio se
    // tratará por separado, ya que implica re-confirmación).
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Miembro desde")]
    public DateTime CreatedAt { get; set; }
}

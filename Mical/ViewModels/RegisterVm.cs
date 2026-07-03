using System.ComponentModel.DataAnnotations;

namespace Mical.ViewModels;

/// <summary>Datos del formulario de registro de un nuevo usuario.</summary>
public class RegisterVm
{
    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [Display(Name = "Nombre y apellido")]
    [StringLength(150, ErrorMessage = "Máximo 150 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu email.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá una contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

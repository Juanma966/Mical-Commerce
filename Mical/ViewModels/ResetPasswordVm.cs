using System.ComponentModel.DataAnnotations;

namespace Mical.ViewModels;

/// <summary>Formulario para establecer una nueva contraseña con el token de recuperación.</summary>
public class ResetPasswordVm
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Token de recuperación de Identity (codificado en Base64Url).</summary>
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repetí la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir nueva contraseña")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

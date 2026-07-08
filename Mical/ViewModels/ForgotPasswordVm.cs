using System.ComponentModel.DataAnnotations;

namespace Mical.ViewModels;

/// <summary>Formulario para solicitar el email de recuperación de contraseña.</summary>
public class ForgotPasswordVm
{
    [Required(ErrorMessage = "Ingresá tu email.")]
    [EmailAddress(ErrorMessage = "Ingresá un email válido.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

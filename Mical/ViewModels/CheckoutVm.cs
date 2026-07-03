using System.ComponentModel.DataAnnotations;

namespace Mical.ViewModels;

/// <summary>Datos de contacto/envío del checkout + carrito serializado.</summary>
public class CheckoutVm
{
    [Required(ErrorMessage = "Ingresá un nombre de contacto.")]
    [StringLength(150)]
    [Display(Name = "Nombre de contacto")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá un teléfono.")]
    [StringLength(30)]
    [Phone(ErrorMessage = "El teléfono no es válido.")]
    [Display(Name = "Teléfono")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá la dirección de envío.")]
    [StringLength(300)]
    [Display(Name = "Dirección de envío")]
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>Carrito serializado desde LocalStorage: JSON de [{productId, quantity}].</summary>
    public string CartJson { get; set; } = "[]";
}

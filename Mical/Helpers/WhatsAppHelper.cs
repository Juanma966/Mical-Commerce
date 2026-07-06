using System.Text;
using Mical.ViewModels;

namespace Mical.Helpers;

/// <summary>
/// Arma enlaces de WhatsApp (wa.me) para que el cliente confirme su pedido
/// al negocio. El mensaje se genera del lado del servidor con datos ya
/// validados del pedido.
/// </summary>
public static class WhatsAppHelper
{
    /// <summary>Deja solo los dígitos del número (wa.me no acepta símbolos).</summary>
    public static string DigitsOnly(string? raw) =>
        new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>Texto legible del pedido, con formato de WhatsApp (*negrita*).</summary>
    public static string BuildOrderMessage(OrderDetailVm order)
    {
        var sb = new StringBuilder();
        sb.Append("¡Hola Mical! Quiero confirmar mi pedido *").Append(order.OrderNumber).Append("*.\n\n");
        sb.Append("🛒 Productos:\n");
        foreach (var i in order.Items)
        {
            sb.Append("• ").Append(i.Quantity).Append("x ").Append(i.ProductName)
              .Append(" — ").Append(i.LineTotal.ToMoney()).Append('\n');
        }
        sb.Append("\n💰 Total: ").Append(order.Total.ToMoney()).Append('\n');
        sb.Append("\n📦 Envío a: ").Append(order.ShippingAddress).Append('\n');
        sb.Append("👤 ").Append(order.ContactName).Append(" — ").Append(order.ContactPhone);
        return sb.ToString();
    }

    /// <summary>
    /// URL completa de wa.me con el pedido pre-cargado. Devuelve null si no hay
    /// número de negocio configurado.
    /// </summary>
    public static string? BuildOrderLink(string? businessNumber, OrderDetailVm order)
    {
        var digits = DigitsOnly(businessNumber);
        if (string.IsNullOrEmpty(digits))
            return null;

        var text = Uri.EscapeDataString(BuildOrderMessage(order));
        return $"https://wa.me/{digits}?text={text}";
    }
}

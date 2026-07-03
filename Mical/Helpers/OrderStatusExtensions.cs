using Mical.Entities;

namespace Mical.Helpers;

public static class OrderStatusExtensions
{
    /// <summary>Clase CSS del badge y etiqueta legible para un estado de pedido.</summary>
    public static (string Css, string Text) Badge(this OrderStatus status) => status switch
    {
        OrderStatus.Pendiente => ("bg-secondary", "Pendiente"),
        OrderStatus.Pagado => ("bg-info text-dark", "Pagado"),
        OrderStatus.Preparando => ("bg-primary", "Preparando"),
        OrderStatus.Enviado => ("bg-warning text-dark", "Enviado"),
        OrderStatus.Entregado => ("bg-success", "Entregado"),
        OrderStatus.Cancelado => ("bg-danger", "Cancelado"),
        _ => ("bg-secondary", status.ToString())
    };
}

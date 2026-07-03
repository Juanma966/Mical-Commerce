namespace Mical.Entities;

/// <summary>
/// Pedido de compra. Contiene sus <see cref="OrderItem"/> con snapshots de
/// nombre y precio. Los datos de envío van embebidos (sin tabla aparte por ahora).
/// </summary>
public class Order
{
    public int Id { get; set; }

    /// <summary>Número legible y único (ej. ORD-2026-000123).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Dueño del pedido (AspNetUsers.Id).</summary>
    public string UserId { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pendiente;

    /// <summary>Total calculado en el servidor al momento de la compra.</summary>
    public decimal Total { get; set; }

    // Datos de contacto/envío (snapshot al momento de comprar).
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

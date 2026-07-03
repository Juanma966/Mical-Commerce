namespace Mical.Entities;

/// <summary>
/// Línea de un pedido. Guarda snapshots inmutables del nombre y el precio
/// cobrado, para no depender de cambios futuros del producto.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Snapshot del nombre del producto al momento de la compra.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Snapshot del precio unitario cobrado.</summary>
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}

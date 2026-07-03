using Mical.Entities;
using Mical.ViewModels;

namespace Mical.Areas.Admin.Models;

/// <summary>Fila del listado de pedidos en el panel admin.</summary>
public class AdminOrderListItemVm
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Detalle de pedido en el panel admin, con las transiciones permitidas.</summary>
public class AdminOrderDetailVm
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemVm> Items { get; set; } = new();

    /// <summary>Estados a los que se puede pasar desde el actual (vacío si es terminal).</summary>
    public IReadOnlyList<OrderStatus> AllowedTransitions { get; set; } = new List<OrderStatus>();
}

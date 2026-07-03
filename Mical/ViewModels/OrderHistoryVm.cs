using Mical.Entities;

namespace Mical.ViewModels;

/// <summary>Fila del historial "Mis pedidos".</summary>
public class OrderHistoryVm
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

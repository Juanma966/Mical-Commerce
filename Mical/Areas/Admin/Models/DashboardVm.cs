using Mical.Entities;

namespace Mical.Areas.Admin.Models;

public class DashboardVm
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }

    /// <summary>Ingresos de pedidos no cancelados.</summary>
    public decimal Revenue { get; set; }

    public List<StatusCountVm> OrdersByStatus { get; set; } = new();
    public List<LowStockItemVm> LowStock { get; set; } = new();
    public List<RecentOrderVm> RecentOrders { get; set; } = new();
}

public class StatusCountVm
{
    public OrderStatus Status { get; set; }
    public int Count { get; set; }
}

public class LowStockItemVm
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinStock { get; set; }
}

public class RecentOrderVm
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

using Mical.Entities;

namespace Mical.Areas.Admin.Models;

public class DashboardVm
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }

    /// <summary>Ingresos de pedidos no cancelados (histórico).</summary>
    public decimal Revenue { get; set; }

    /// <summary>Ingresos de pedidos no cancelados del mes en curso.</summary>
    public decimal RevenueThisMonth { get; set; }

    /// <summary>Pedidos creados en el mes en curso.</summary>
    public int OrdersThisMonth { get; set; }

    /// <summary>Pedidos en estado Pendiente (requieren atención).</summary>
    public int PendingOrders { get; set; }

    /// <summary>Cantidad de pedidos no cancelados (base del ticket promedio).</summary>
    public int NonCancelledOrders { get; set; }

    /// <summary>Clientes registrados (usuarios).</summary>
    public int TotalCustomers { get; set; }

    /// <summary>Productos activos sin stock.</summary>
    public int OutOfStockCount { get; set; }

    /// <summary>Promociones activas.</summary>
    public int ActivePromotions { get; set; }

    /// <summary>Ticket promedio de los pedidos no cancelados.</summary>
    public decimal AverageOrderValue => NonCancelledOrders > 0 ? Revenue / NonCancelledOrders : 0m;

    public List<DailySalesVm> SalesLast7Days { get; set; } = new();
    public List<StatusCountVm> OrdersByStatus { get; set; } = new();
    public List<LowStockItemVm> LowStock { get; set; } = new();
    public List<RecentOrderVm> RecentOrders { get; set; } = new();
}

public class DailySalesVm
{
    public DateOnly Date { get; set; }
    public decimal Total { get; set; }
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

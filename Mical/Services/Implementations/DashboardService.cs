using Mical.Areas.Admin.Models;
using Mical.Data;
using Mical.Entities;
using Mical.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mical.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardVm> GetAsync()
    {
        var vm = new DashboardVm
        {
            TotalProducts = await _db.Products.CountAsync(),
            TotalCategories = await _db.Categories.CountAsync(),
            TotalOrders = await _db.Orders.CountAsync(),
            Revenue = await _db.Orders
                .Where(o => o.Status != OrderStatus.Cancelado)
                .SumAsync(o => (decimal?)o.Total) ?? 0m
        };

        // Conteo por estado (todos los estados, con 0 si no hay).
        var counts = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();

        vm.OrdersByStatus = Enum.GetValues<OrderStatus>()
            .Select(s => new StatusCountVm
            {
                Status = s,
                Count = counts.FirstOrDefault(c => c.Key == s)?.Count ?? 0
            })
            .ToList();

        // Productos en o por debajo del stock mínimo (visibles).
        vm.LowStock = await _db.Products
            .Where(p => p.IsActive && p.Stock <= p.MinStock)
            .OrderBy(p => p.Stock)
            .Select(p => new LowStockItemVm
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Stock = p.Stock,
                MinStock = p.MinStock
            })
            .Take(10)
            .ToListAsync();

        // Últimos pedidos.
        vm.RecentOrders = await _db.Orders
            .OrderByDescending(o => o.Id)
            .Take(5)
            .Select(o => new RecentOrderVm
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerEmail = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.Email!).FirstOrDefault() ?? "",
                Status = o.Status,
                Total = o.Total,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return vm;
    }
}

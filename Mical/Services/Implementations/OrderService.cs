using System.Text.Json;
using Mical.Areas.Admin.Models;
using Mical.Data;
using Mical.Entities;
using Mical.Models;
using Mical.Services.Interfaces;
using Mical.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Mical.Services.Implementations;

public class OrderService : IOrderService
{
    private const int MaxConcurrencyRetries = 3;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Transiciones permitidas de la máquina de estados del pedido.</summary>
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pendiente] = new[] { OrderStatus.Pagado, OrderStatus.Cancelado },
        [OrderStatus.Pagado] = new[] { OrderStatus.Preparando, OrderStatus.Cancelado },
        [OrderStatus.Preparando] = new[] { OrderStatus.Enviado, OrderStatus.Cancelado },
        [OrderStatus.Enviado] = new[] { OrderStatus.Entregado, OrderStatus.Cancelado },
        [OrderStatus.Entregado] = new[] { OrderStatus.Cancelado },
        [OrderStatus.Cancelado] = Array.Empty<OrderStatus>()
    };

    private readonly ApplicationDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ApplicationDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OperationResult<int>> CheckoutAsync(string userId, CheckoutVm model)
    {
        // Parseo del carrito que viene del cliente (solo id + cantidad).
        var requested = ParseCart(model.CartJson);
        if (requested.Count == 0)
            return OperationResult<int>.Fail("Tu carrito está vacío.");

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await TryCheckoutAsync(userId, model, requested);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // Otro pedido tocó el stock de un producto entre la lectura y el guardado.
                _logger.LogWarning("Conflicto de concurrencia en checkout (intento {Attempt}). Reintentando.", attempt);
                _db.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException)
            {
                return OperationResult<int>.Fail(
                    "Otro cliente compró al mismo tiempo y cambió el stock. Revisá tu carrito e intentá de nuevo.");
            }
        }
    }

    private async Task<OperationResult<int>> TryCheckoutAsync(
        string userId, CheckoutVm model, Dictionary<int, int> requested)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var ids = requested.Keys.ToList();
        var products = await _db.Products
            .Where(p => ids.Contains(p.Id) && p.IsActive && p.Category!.IsActive)
            .ToListAsync();

        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.Pendiente,
            ContactName = model.ContactName.Trim(),
            ContactPhone = model.ContactPhone.Trim(),
            ShippingAddress = model.ShippingAddress.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        decimal total = 0;
        foreach (var (productId, qty) in requested)
        {
            var product = products.FirstOrDefault(p => p.Id == productId);
            if (product is null)
                return OperationResult<int>.Fail("Un producto de tu carrito ya no está disponible. Revisá el carrito.");

            if (product.Stock < qty)
                return OperationResult<int>.Fail(
                    $"No hay stock suficiente de “{product.Name}” (disponible: {product.Stock}).");

            var unitPrice = product.SalePrice ?? product.Price;
            var lineTotal = unitPrice * qty;
            total += lineTotal;

            // Descuento de stock (la concurrencia optimista via xmin evita sobreventa).
            product.Stock -= qty;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,   // snapshot
                UnitPrice = unitPrice,        // snapshot
                Quantity = qty,
                LineTotal = lineTotal
            });
        }

        order.Total = total;
        order.OrderNumber = await NextOrderNumberAsync();

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Pedido creado {OrderNumber} (Id {Id}) por {User}, total {Total}.",
            order.OrderNumber, order.Id, userId, total);

        return OperationResult<int>.Success(order.Id);
    }

    public async Task<OrderDetailVm?> GetForUserAsync(int orderId, string userId)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new OrderDetailVm
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                Total = o.Total,
                ContactName = o.ContactName,
                ContactPhone = o.ContactPhone,
                ShippingAddress = o.ShippingAddress,
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new OrderItemVm
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<OrderHistoryVm>> GetHistoryForUserAsync(string userId)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.Id)
            .Select(o => new OrderHistoryVm
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                Total = o.Total,
                ItemCount = o.Items.Sum(i => i.Quantity),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminOrderListItemVm>> GetAllForAdminAsync()
    {
        return await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.Id)
            .Select(o => new AdminOrderListItemVm
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerEmail = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.Email!).FirstOrDefault() ?? "",
                Status = o.Status,
                Total = o.Total,
                ItemCount = o.Items.Sum(i => i.Quantity),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AdminOrderDetailVm?> GetForAdminAsync(int orderId)
    {
        var vm = await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(o => new AdminOrderDetailVm
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerEmail = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.Email!).FirstOrDefault() ?? "",
                Status = o.Status,
                Total = o.Total,
                ContactName = o.ContactName,
                ContactPhone = o.ContactPhone,
                ShippingAddress = o.ShippingAddress,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                Items = o.Items.Select(i => new OrderItemVm
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (vm is not null)
            vm.AllowedTransitions = AllowedTransitions[vm.Status];

        return vm;
    }

    public async Task<OperationResult> UpdateStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
            return OperationResult.Fail("El pedido no existe.");

        var current = order.Status;
        if (!AllowedTransitions[current].Contains(newStatus))
            return OperationResult.Fail($"No se puede pasar de {current} a {newStatus}.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        // Reponer stock solo al cancelar y solo si NO estaba Entregado.
        if (newStatus == OrderStatus.Cancelado && current != OrderStatus.Entregado)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            // IgnoreQueryFilters: reponer aunque el producto esté soft-deleted.
            var products = await _db.Products
                .IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product is not null)
                    product.Stock += item.Quantity;
            }
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return OperationResult.Success();
    }

    private async Task<string> NextOrderNumberAsync()
    {
        var next = await _db.Database
            .SqlQueryRaw<long>("SELECT nextval('order_number_seq') AS \"Value\"")
            .SingleAsync();
        return $"ORD-{DateTime.UtcNow:yyyy}-{next:D6}";
    }

    private static Dictionary<int, int> ParseCart(string? cartJson)
    {
        if (string.IsNullOrWhiteSpace(cartJson))
            return new();

        try
        {
            var items = JsonSerializer.Deserialize<List<CartItemInput>>(cartJson, JsonOpts) ?? new();
            return items
                .Where(i => i.ProductId > 0 && i.Quantity > 0)
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => Math.Min(g.Sum(x => x.Quantity), 999));
        }
        catch (JsonException)
        {
            return new();
        }
    }
}

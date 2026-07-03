using System.Text.Json;
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

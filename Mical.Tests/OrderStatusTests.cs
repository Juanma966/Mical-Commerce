using Mical.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mical.Tests;

/// <summary>
/// Behavior of <c>OrderService.UpdateStatusAsync</c>: the state machine and the
/// rule that decides whether cancelling gives the stock back.
/// </summary>
public class OrderStatusTests : OrderServiceTestBase
{
    public OrderStatusTests(PostgresFixture db) : base(db) { }

    /// <summary>Places a real order of <paramref name="quantity"/> units and walks it to <paramref name="status"/>.</summary>
    private async Task<int> PlaceOrderAsync(int productId, int quantity = 2, OrderStatus status = OrderStatus.Pendiente)
    {
        var userId = await SeedUserAsync($"cliente-{Guid.NewGuid():N}@test.com");

        int orderId;
        await using (var db = Db.CreateContext())
        {
            var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, quantity))));
            Assert.True(result.Succeeded, result.Error);
            orderId = result.Value;
        }

        // Walk the state machine forward one legal step at a time.
        var path = new[]
        {
            OrderStatus.Pagado, OrderStatus.Preparando, OrderStatus.Enviado, OrderStatus.Entregado
        };

        foreach (var step in path.TakeWhile(s => s <= status))
        {
            await using var db = Db.CreateContext();
            var moved = await NewService(db).UpdateStatusAsync(orderId, step);
            Assert.True(moved.Succeeded, moved.Error);
        }

        return orderId;
    }

    private async Task<OrderStatus> StatusOfAsync(int orderId)
    {
        await using var db = Db.CreateContext();
        return await db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync();
    }

    [Fact]
    public async Task A_legal_transition_moves_the_order_and_stamps_UpdatedAt()
    {
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderAsync(productId);

        await using var db = Db.CreateContext();
        var result = await NewService(db).UpdateStatusAsync(orderId, OrderStatus.Pagado);

        Assert.True(result.Succeeded, result.Error);

        await using var check = Db.CreateContext();
        var order = await check.Orders.SingleAsync(o => o.Id == orderId);

        Assert.Equal(OrderStatus.Pagado, order.Status);
        Assert.NotNull(order.UpdatedAt);
    }

    [Theory]
    [InlineData(OrderStatus.Preparando)]
    [InlineData(OrderStatus.Enviado)]
    [InlineData(OrderStatus.Entregado)]
    public async Task Skipping_a_step_is_rejected_and_leaves_the_status_alone(OrderStatus target)
    {
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderAsync(productId);

        await using var db = Db.CreateContext();
        var result = await NewService(db).UpdateStatusAsync(orderId, target);

        Assert.False(result.Succeeded);
        Assert.Contains("No se puede pasar", result.Error);
        Assert.Equal(OrderStatus.Pendiente, await StatusOfAsync(orderId));
    }

    [Fact]
    public async Task A_cancelled_order_is_terminal()
    {
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderAsync(productId);

        await using (var db = Db.CreateContext())
        {
            var cancelled = await NewService(db).UpdateStatusAsync(orderId, OrderStatus.Cancelado);
            Assert.True(cancelled.Succeeded, cancelled.Error);
        }

        await using var retry = Db.CreateContext();
        var result = await NewService(retry).UpdateStatusAsync(orderId, OrderStatus.Pagado);

        Assert.False(result.Succeeded);
        Assert.Equal(OrderStatus.Cancelado, await StatusOfAsync(orderId));
    }

    [Fact]
    public async Task Cancelling_a_paid_order_puts_the_stock_back()
    {
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderAsync(productId, quantity: 3, status: OrderStatus.Pagado);

        Assert.Equal(7, await StockOfAsync(productId));

        await using var db = Db.CreateContext();
        var result = await NewService(db).UpdateStatusAsync(orderId, OrderStatus.Cancelado);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(10, await StockOfAsync(productId));
    }

    /// <summary>
    /// The business rule that is easiest to break by accident: once the goods are
    /// delivered, cancelling is bookkeeping, not a restock.
    /// </summary>
    [Fact]
    public async Task Cancelling_a_delivered_order_does_NOT_put_the_stock_back()
    {
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderAsync(productId, quantity: 3, status: OrderStatus.Entregado);

        Assert.Equal(7, await StockOfAsync(productId));

        await using var db = Db.CreateContext();
        var result = await NewService(db).UpdateStatusAsync(orderId, OrderStatus.Cancelado);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(OrderStatus.Cancelado, await StatusOfAsync(orderId));
        Assert.Equal(7, await StockOfAsync(productId));
    }

    [Fact]
    public async Task Cancelling_restocks_even_a_product_that_was_soft_deleted_afterwards()
    {
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderAsync(productId, quantity: 4, status: OrderStatus.Pagado);

        await using (var remove = Db.CreateContext())
        {
            var product = await remove.Products.SingleAsync(p => p.Id == productId);
            product.IsDeleted = true;
            await remove.SaveChangesAsync();
        }

        await using var db = Db.CreateContext();
        var result = await NewService(db).UpdateStatusAsync(orderId, OrderStatus.Cancelado);

        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(10, await StockOfAsync(productId));
    }

    [Fact]
    public async Task Updating_a_non_existent_order_fails()
    {
        await using var db = Db.CreateContext();
        var result = await NewService(db).UpdateStatusAsync(9999, OrderStatus.Pagado);

        Assert.False(result.Succeeded);
        Assert.Contains("no existe", result.Error);
    }
}

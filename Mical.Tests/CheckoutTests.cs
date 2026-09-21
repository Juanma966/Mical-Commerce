using Mical.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mical.Tests;

/// <summary>
/// Behavior of <c>OrderService.CheckoutAsync</c>: what the customer ends up with,
/// and what the stock looks like afterwards.
/// </summary>
public class CheckoutTests : OrderServiceTestBase
{
    public CheckoutTests(PostgresFixture db) : base(db) { }

    [Fact]
    public async Task Checkout_with_a_valid_cart_creates_the_order_and_deducts_stock()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, price: 1500m, stock: 10);

        await using var db = Db.CreateContext();
        var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, 3))));

        Assert.True(result.Succeeded, result.Error);

        await using var check = Db.CreateContext();
        var order = await check.Orders.Include(o => o.Items).SingleAsync();

        Assert.Equal(OrderStatus.Pendiente, order.Status);
        Assert.Equal(userId, order.UserId);
        Assert.Equal(4500m, order.Total);
        Assert.Equal(3, Assert.Single(order.Items).Quantity);
        Assert.Equal(7, await StockOfAsync(productId));
    }

    [Fact]
    public async Task Checkout_prices_the_order_with_the_sale_price_not_the_list_price()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, price: 1000m, salePrice: 800m, stock: 5);

        await using var db = Db.CreateContext();
        var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, 2))));

        Assert.True(result.Succeeded, result.Error);

        await using var check = Db.CreateContext();
        var item = await check.OrderItems.SingleAsync();

        Assert.Equal(800m, item.UnitPrice);
        Assert.Equal(1600m, item.LineTotal);
    }

    [Fact]
    public async Task Checkout_snapshots_the_product_name_so_later_renames_do_not_rewrite_history()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, name: "Taza original", stock: 5);

        await using (var db = Db.CreateContext())
        {
            var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, 1))));
            Assert.True(result.Succeeded, result.Error);
        }

        await using (var rename = Db.CreateContext())
        {
            var product = await rename.Products.SingleAsync(p => p.Id == productId);
            product.Name = "Taza renombrada";
            await rename.SaveChangesAsync();
        }

        await using var check = Db.CreateContext();
        Assert.Equal("Taza original", (await check.OrderItems.SingleAsync()).ProductName);
    }

    /// <summary>
    /// The important one: a partially-fillable cart must leave the database exactly
    /// as it was. The first line is deductible, the second is not.
    /// </summary>
    [Fact]
    public async Task Checkout_without_enough_stock_fails_and_leaves_every_stock_untouched()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var plentiful = await SeedProductAsync(categoryId, name: "Con stock", stock: 10);
        var scarce = await SeedProductAsync(categoryId, name: "Casi agotado", stock: 1);

        await using var db = Db.CreateContext();
        var result = await NewService(db)
            .CheckoutAsync(userId, Checkout(CartJson((plentiful, 2), (scarce, 5))));

        Assert.False(result.Succeeded);
        Assert.Contains("Casi agotado", result.Error);

        await using var check = Db.CreateContext();
        Assert.Empty(await check.Orders.ToListAsync());
        Assert.Equal(10, await StockOfAsync(plentiful));
        Assert.Equal(1, await StockOfAsync(scarce));
    }

    [Fact]
    public async Task Checkout_rejects_an_inactive_product()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, isActive: false);

        await using var db = Db.CreateContext();
        var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, 1))));

        Assert.False(result.Succeeded);
        Assert.Contains("ya no está disponible", result.Error);
    }

    [Fact]
    public async Task Checkout_rejects_a_product_whose_category_was_deactivated()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync(isActive: false);
        var productId = await SeedProductAsync(categoryId);

        await using var db = Db.CreateContext();
        var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, 1))));

        Assert.False(result.Succeeded);
        Assert.Contains("ya no está disponible", result.Error);
    }

    [Fact]
    public async Task Checkout_rejects_a_soft_deleted_product()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, isDeleted: true);

        await using var db = Db.CreateContext();
        var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, 1))));

        Assert.False(result.Succeeded);
        Assert.Contains("ya no está disponible", result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("no soy json")]
    [InlineData("{\"productId\": 1}")]
    public async Task Checkout_rejects_an_empty_or_unparseable_cart(string cartJson)
    {
        var userId = await SeedUserAsync();

        await using var db = Db.CreateContext();
        var result = await NewService(db).CheckoutAsync(userId, Checkout(cartJson));

        Assert.False(result.Succeeded);
        Assert.Contains("vacío", result.Error);
    }

    [Fact]
    public async Task Checkout_discards_non_positive_quantities()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);

        await using var db = Db.CreateContext();
        var result = await NewService(db)
            .CheckoutAsync(userId, Checkout(CartJson((productId, 0), (productId, -5))));

        Assert.False(result.Succeeded);
        Assert.Contains("vacío", result.Error);
        Assert.Equal(10, await StockOfAsync(productId));
    }

    [Fact]
    public async Task Checkout_merges_duplicate_lines_for_the_same_product()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, price: 100m, stock: 10);

        await using var db = Db.CreateContext();
        var result = await NewService(db)
            .CheckoutAsync(userId, Checkout(CartJson((productId, 2), (productId, 3))));

        Assert.True(result.Succeeded, result.Error);

        await using var check = Db.CreateContext();
        var item = Assert.Single(await check.OrderItems.ToListAsync());

        Assert.Equal(5, item.Quantity);
        Assert.Equal(500m, item.LineTotal);
        Assert.Equal(5, await StockOfAsync(productId));
    }

    [Fact]
    public async Task Checkout_numbers_orders_sequentially()
    {
        var userId = await SeedUserAsync();
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);

        for (var i = 0; i < 2; i++)
        {
            await using var db = Db.CreateContext();
            var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, 1))));
            Assert.True(result.Succeeded, result.Error);
        }

        await using var check = Db.CreateContext();
        var numbers = await check.Orders.OrderBy(o => o.Id).Select(o => o.OrderNumber).ToListAsync();

        var year = DateTime.UtcNow.Year;
        Assert.Equal(new[] { $"ORD-{year}-000001", $"ORD-{year}-000002" }, numbers);
    }
}

using Mical.Entities;

namespace Mical.Tests;

/// <summary>
/// One customer must never be able to read another customer's order, whatever
/// id they type into the URL.
/// </summary>
public class OrderOwnershipTests : OrderServiceTestBase
{
    public OrderOwnershipTests(PostgresFixture db) : base(db) { }

    private async Task<int> PlaceOrderForAsync(string userId, int productId, int quantity = 1)
    {
        await using var db = Db.CreateContext();
        var result = await NewService(db).CheckoutAsync(userId, Checkout(CartJson((productId, quantity))));
        Assert.True(result.Succeeded, result.Error);
        return result.Value;
    }

    [Fact]
    public async Task The_owner_can_read_their_own_order()
    {
        var owner = await SeedUserAsync("duenio@test.com");
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, price: 250m, stock: 10);
        var orderId = await PlaceOrderForAsync(owner, productId, quantity: 2);

        await using var db = Db.CreateContext();
        var vm = await NewService(db).GetForUserAsync(orderId, owner);

        Assert.NotNull(vm);
        Assert.Equal(500m, vm.Total);
        Assert.Single(vm.Items);
    }

    [Fact]
    public async Task A_stranger_asking_for_someone_elses_order_gets_nothing()
    {
        var owner = await SeedUserAsync("duenio@test.com");
        var stranger = await SeedUserAsync("ajeno@test.com");
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderForAsync(owner, productId);

        await using var db = Db.CreateContext();

        Assert.Null(await NewService(db).GetForUserAsync(orderId, stranger));
    }

    [Fact]
    public async Task The_history_only_lists_the_users_own_orders_newest_first()
    {
        var owner = await SeedUserAsync("duenio@test.com");
        var other = await SeedUserAsync("otro@test.com");
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 20);

        var first = await PlaceOrderForAsync(owner, productId);
        var second = await PlaceOrderForAsync(owner, productId);
        await PlaceOrderForAsync(other, productId);

        await using var db = Db.CreateContext();
        var history = await NewService(db).GetHistoryForUserAsync(owner);

        Assert.Equal(new[] { second, first }, history.Select(o => o.Id).ToArray());
    }

    [Fact]
    public async Task The_admin_view_sees_every_order_with_the_customer_email()
    {
        var owner = await SeedUserAsync("duenio@test.com");
        var other = await SeedUserAsync("otro@test.com");
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 20);

        await PlaceOrderForAsync(owner, productId);
        await PlaceOrderForAsync(other, productId);

        await using var db = Db.CreateContext();
        var all = await NewService(db).GetAllForAdminAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains("duenio@test.com", all.Select(o => o.CustomerEmail));
        Assert.Contains("otro@test.com", all.Select(o => o.CustomerEmail));
    }

    [Fact]
    public async Task The_admin_detail_offers_only_the_transitions_the_state_machine_allows()
    {
        var owner = await SeedUserAsync("duenio@test.com");
        var categoryId = await SeedCategoryAsync();
        var productId = await SeedProductAsync(categoryId, stock: 10);
        var orderId = await PlaceOrderForAsync(owner, productId);

        await using var db = Db.CreateContext();
        var vm = await NewService(db).GetForAdminAsync(orderId);

        Assert.NotNull(vm);
        Assert.Equal(
            new[] { OrderStatus.Pagado, OrderStatus.Cancelado },
            vm.AllowedTransitions);
    }
}

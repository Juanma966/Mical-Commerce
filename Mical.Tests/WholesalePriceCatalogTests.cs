using Mical.Data;
using Mical.Entities;
using Mical.Services.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Mical.Tests;

/// <summary>
/// The wholesale price has to survive the round trip to PostgreSQL and reach the
/// public catalog projections, which is where the customer actually sees it.
/// </summary>
[Collection(PostgresCollection.Name)]
public class WholesalePriceCatalogTests : IAsyncLifetime
{
    private readonly PostgresFixture _db;

    public WholesalePriceCatalogTests(PostgresFixture db) => _db = db;

    public Task InitializeAsync() => _db.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static CatalogService NewCatalog(ApplicationDbContext db) => new(db);

    private async Task<int> SeedProductAsync(decimal? wholesalePrice, decimal price = 1000m)
    {
        await using var db = _db.CreateContext();

        var category = new Category { Name = "Regalería", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Sku = $"PRD-W-{Guid.NewGuid():N}"[..18],
            Name = "Taza personalizada",
            Price = price,
            WholesalePrice = wholesalePrice,
            CategoryId = category.Id,
            Stock = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    [Fact]
    public async Task The_wholesale_price_round_trips_through_the_database()
    {
        var productId = await SeedProductAsync(649.50m);

        await using var db = _db.CreateContext();
        var stored = await db.Products.SingleAsync(p => p.Id == productId);

        // numeric(12,2): los centavos no se redondean ni se pierden.
        Assert.Equal(649.50m, stored.WholesalePrice);
    }

    [Fact]
    public async Task The_catalog_card_carries_the_wholesale_price()
    {
        await SeedProductAsync(800m, price: 1200m);

        await using var db = _db.CreateContext();
        var shop = await NewCatalog(db).GetShopAsync(categoryId: null, query: null, page: 1, pageSize: 12);

        var card = Assert.Single(shop.Products.Items);
        Assert.Equal(800m, card.WholesalePrice);
        Assert.Equal(1200m, card.EffectivePrice);
    }

    [Fact]
    public async Task The_product_detail_carries_the_wholesale_price()
    {
        var productId = await SeedProductAsync(800m);

        await using var db = _db.CreateContext();
        var detail = await NewCatalog(db).GetProductDetailAsync(productId);

        Assert.NotNull(detail);
        Assert.Equal(800m, detail.WholesalePrice);
    }

    /// <summary>
    /// Products loaded before the field existed have no wholesale price. They must
    /// read back as null so the view simply omits the line, instead of showing a
    /// fabricated $0 to customers.
    /// </summary>
    [Fact]
    public async Task A_product_from_before_the_field_reads_back_as_null()
    {
        await SeedProductAsync(null);

        await using var db = _db.CreateContext();
        var shop = await NewCatalog(db).GetShopAsync(categoryId: null, query: null, page: 1, pageSize: 12);

        Assert.Null(Assert.Single(shop.Products.Items).WholesalePrice);
    }
}

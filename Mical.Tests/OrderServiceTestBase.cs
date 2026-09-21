using System.Text.Json;
using Mical.Data;
using Mical.Entities;
using Microsoft.EntityFrameworkCore;
using Mical.Services.Implementations;
using Mical.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mical.Tests;

/// <summary>
/// Shared setup for the OrderService suite: a clean database per test plus
/// small builders for the fixtures each test needs.
/// </summary>
[Collection(PostgresCollection.Name)]
public abstract class OrderServiceTestBase : IAsyncLifetime
{
    protected readonly PostgresFixture Db;

    protected OrderServiceTestBase(PostgresFixture db) => Db = db;

    public Task InitializeAsync() => Db.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>A service bound to its own context, like a real web request.</summary>
    protected static OrderService NewService(ApplicationDbContext db) =>
        new(db, NullLogger<OrderService>.Instance);

    protected async Task<string> SeedUserAsync(string email = "cliente@test.com")
    {
        await using var db = Db.CreateContext();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            FullName = "Cliente de prueba",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    protected async Task<int> SeedCategoryAsync(string name = "Regalería", bool isActive = true)
    {
        await using var db = Db.CreateContext();

        var category = new Category
        {
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    protected async Task<int> SeedProductAsync(
        int categoryId,
        string name = "Taza personalizada",
        decimal price = 1000m,
        decimal? salePrice = null,
        int stock = 10,
        bool isActive = true,
        bool isDeleted = false)
    {
        await using var db = Db.CreateContext();

        var product = new Product
        {
            Sku = $"PRD-TEST-{Guid.NewGuid():N}"[..20],
            Name = name,
            Price = price,
            SalePrice = salePrice,
            CategoryId = categoryId,
            Stock = stock,
            MinStock = 0,
            IsActive = isActive,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    /// <summary>Serializes a cart the way the browser posts it: id + quantity only.</summary>
    protected static string CartJson(params (int ProductId, int Quantity)[] items) =>
        JsonSerializer.Serialize(items.Select(i => new CartItemInput
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }));

    protected static CheckoutVm Checkout(string cartJson) => new()
    {
        ContactName = "Juan Pérez",
        ContactPhone = "2646277552",
        ShippingAddress = "Av. Libertador 123, San Juan",
        CartJson = cartJson
    };

    /// <summary>Reads the current stock straight from the database.</summary>
    protected async Task<int> StockOfAsync(int productId)
    {
        await using var db = Db.CreateContext();
        return await db.Products
            .IgnoreQueryFilters()
            .Where(p => p.Id == productId)
            .Select(p => p.Stock)
            .SingleAsync();
    }
}

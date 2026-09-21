using Mical.Data;
using Mical.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;

namespace Mical.Tests;

/// <summary>
/// Spins up a throwaway PostgreSQL container for the whole test run and applies
/// the real EF migrations to it.
///
/// A real PostgreSQL is not a luxury here: the checkout relies on transactions,
/// on the <c>order_number_seq</c> sequence and on the <c>xmin</c> optimistic
/// concurrency token. The EF in-memory provider silently ignores transactions,
/// so testing rollback against it would produce green tests that prove nothing.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Same major version as production.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("mical_test")
        .WithUsername("mical")
        .WithPassword("mical_test")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// A fresh context per call. Assertions must always use a new context so they
    /// read what actually reached the database, never the change tracker.
    /// </summary>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            // Mirrors the production registration in Program.cs.
            .ConfigureWarnings(w => w.Ignore(
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Wipes domain data and restarts the sequences so each test starts from a
    /// known state (order numbers included).
    /// </summary>
    public async Task ResetAsync()
    {
        await using var db = CreateContext();

        await db.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE "OrderItems", "Orders", "Products", "Categories",
                           "AuditLogs", "Promotions", "AspNetUsers"
            RESTART IDENTITY CASCADE;
            """);

        await db.Database.ExecuteSqlRawAsync("ALTER SEQUENCE order_number_seq RESTART WITH 1;");
        await db.Database.ExecuteSqlRawAsync("ALTER SEQUENCE product_sku_seq RESTART WITH 1;");
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}

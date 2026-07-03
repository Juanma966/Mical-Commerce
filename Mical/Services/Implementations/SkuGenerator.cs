using Mical.Data;
using Mical.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mical.Services.Implementations;

/// <summary>
/// Genera el SKU tomando el siguiente valor de la secuencia de PostgreSQL
/// <c>product_sku_seq</c>. La secuencia es atómica: dos altas simultáneas nunca
/// obtienen el mismo número (se aceptan huecos si un alta falla, es intencional).
/// </summary>
public class SkuGenerator : ISkuGenerator
{
    private readonly ApplicationDbContext _db;

    public SkuGenerator(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync()
    {
        var next = await _db.Database
            .SqlQueryRaw<long>("SELECT nextval('product_sku_seq') AS \"Value\"")
            .SingleAsync();

        return $"PRD-{DateTime.UtcNow:yyyy}-{next:D6}";
    }
}

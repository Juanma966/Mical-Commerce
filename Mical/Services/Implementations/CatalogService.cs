using Mical.Data;
using Mical.Models;
using Mical.Services.Interfaces;
using Mical.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Mical.Services.Implementations;

public class CatalogService : ICatalogService
{
    private const int MaxPageSize = 48;

    private readonly ApplicationDbContext _db;

    public CatalogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ShopIndexVm> GetShopAsync(int? categoryId, string? query, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 12 : pageSize;
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        // Solo productos activos de categorías activas (los borrados los excluye el query filter).
        var products = _db.Products.Where(p => p.IsActive && p.Category!.IsActive);

        if (categoryId is > 0)
            products = products.Where(p => p.CategoryId == categoryId);

        if (query is not null)
            products = products.Where(p => EF.Functions.ILike(p.Name, $"%{query}%"));

        var total = await products.CountAsync();

        var items = await products
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductCardVm
            {
                Id = p.Id,
                Name = p.Name,
                ImagePath = p.ImagePath,
                Price = p.Price,
                EffectivePrice = p.SalePrice ?? p.Price,
                IsOnSale = p.SalePrice != null && p.SalePrice < p.Price,
                IsOutOfStock = p.Stock <= 0
            })
            .ToListAsync();

        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryFilterVm { Id = c.Id, Name = c.Name })
            .ToListAsync();

        return new ShopIndexVm
        {
            Products = new PagedResult<ProductCardVm>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            },
            Categories = categories,
            SelectedCategoryId = categoryId is > 0 ? categoryId : null,
            Query = query
        };
    }

    public async Task<CartVm> RehydrateCartAsync(IEnumerable<CartItemInput> items)
    {
        // Une duplicados y descarta entradas inválidas. La cantidad pedida se limita
        // a un máximo razonable por línea para evitar abusos.
        var requested = items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => Math.Min(g.Sum(x => x.Quantity), 999));

        var result = new CartVm();
        if (requested.Count == 0)
            return result;

        var ids = requested.Keys.ToList();

        // Solo productos visibles (activos y de categorías activas).
        var products = await _db.Products
            .Where(p => ids.Contains(p.Id) && p.IsActive && p.Category!.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ImagePath,
                UnitPrice = p.SalePrice ?? p.Price,
                p.Stock
            })
            .ToListAsync();

        foreach (var id in ids)
        {
            var requestedQty = requested[id];
            var prod = products.FirstOrDefault(p => p.Id == id);

            if (prod is null)
            {
                // Ya no está disponible (borrado, inactivo o categoría inactiva).
                result.Lines.Add(new CartLineVm
                {
                    ProductId = id,
                    Name = "Producto no disponible",
                    RequestedQuantity = requestedQty,
                    Quantity = 0,
                    Available = false
                });
                result.HasIssues = true;
                continue;
            }

            var effectiveQty = Math.Min(requestedQty, prod.Stock);
            var lineTotal = prod.UnitPrice * effectiveQty;

            result.Lines.Add(new CartLineVm
            {
                ProductId = prod.Id,
                Name = prod.Name,
                ImagePath = prod.ImagePath,
                UnitPrice = prod.UnitPrice,
                Quantity = effectiveQty,
                RequestedQuantity = requestedQty,
                AvailableStock = prod.Stock,
                LineTotal = lineTotal,
                Available = true
            });

            result.Total += lineTotal;
            result.ItemCount += effectiveQty;

            if (effectiveQty < requestedQty)
                result.HasIssues = true;
        }

        return result;
    }

    public async Task<ProductDetailVm?> GetProductDetailAsync(int id)
    {
        return await _db.Products
            .Where(p => p.Id == id && p.IsActive && p.Category!.IsActive)
            .Select(p => new ProductDetailVm
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                ImagePath = p.ImagePath,
                Price = p.Price,
                EffectivePrice = p.SalePrice ?? p.Price,
                IsOnSale = p.SalePrice != null && p.SalePrice < p.Price,
                Stock = p.Stock,
                IsOutOfStock = p.Stock <= 0,
                CategoryId = p.CategoryId,
                CategoryName = p.Category!.Name
            })
            .FirstOrDefaultAsync();
    }
}

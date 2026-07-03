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

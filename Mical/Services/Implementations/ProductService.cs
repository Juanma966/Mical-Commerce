using Mical.Areas.Admin.Models;
using Mical.Data;
using Mical.Entities;
using Mical.Models;
using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Mical.Services.Implementations;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;
    private readonly ISkuGenerator _sku;
    private readonly IFileStorageService _files;
    private readonly ILogger<ProductService> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public ProductService(
        ApplicationDbContext db,
        ISkuGenerator sku,
        IFileStorageService files,
        ILogger<ProductService> logger,
        IHttpContextAccessor httpContext)
    {
        _db = db;
        _sku = sku;
        _files = files;
        _logger = logger;
        _httpContext = httpContext;
    }

    private string CurrentUser => _httpContext.HttpContext?.User?.Identity?.Name ?? "sistema";

    public async Task<IReadOnlyList<AdminProductListItemVm>> GetAllForAdminAsync()
    {
        return await _db.Products
            .AsNoTracking()
            .OrderByDescending(p => p.Id)
            .Select(p => new AdminProductListItemVm
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                CategoryName = p.Category!.Name,
                Price = p.Price,
                SalePrice = p.SalePrice,
                Stock = p.Stock,
                MinStock = p.MinStock,
                IsActive = p.IsActive,
                ImagePath = p.ImagePath
            })
            .ToListAsync();
    }

    public async Task<ProductFormVm?> GetForEditAsync(int id)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductFormVm
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                SalePrice = p.SalePrice,
                CategoryId = p.CategoryId,
                Stock = p.Stock,
                MinStock = p.MinStock,
                IsActive = p.IsActive,
                ExistingImagePath = p.ImagePath
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AdminProductListItemVm?> GetByIdAsync(int id)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new AdminProductListItemVm
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                CategoryName = p.Category!.Name,
                Price = p.Price,
                SalePrice = p.SalePrice,
                Stock = p.Stock,
                MinStock = p.MinStock,
                IsActive = p.IsActive,
                ImagePath = p.ImagePath
            })
            .FirstOrDefaultAsync();
    }

    public async Task<OperationResult> CreateAsync(ProductFormVm model)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == model.CategoryId))
            return OperationResult.Fail("La categoría seleccionada no existe.");

        string? imagePath = null;
        if (model.Image is not null)
        {
            var saved = await _files.SaveProductImageAsync(model.Image);
            if (!saved.Succeeded)
                return OperationResult.Fail(saved.Error!);
            imagePath = saved.RelativePath;
        }

        var product = new Product
        {
            Sku = await _sku.GenerateAsync(),
            Name = model.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            Price = model.Price,
            SalePrice = model.SalePrice,
            CategoryId = model.CategoryId,
            Stock = model.Stock,
            MinStock = model.MinStock,
            IsActive = model.IsActive,
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[AUDIT] {User} creó el producto '{Name}' (SKU {Sku}, Id {Id}).",
            CurrentUser, product.Name, product.Sku, product.Id);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateAsync(ProductFormVm model)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (product is null)
            return OperationResult.Fail("El producto no existe.");

        if (!await _db.Categories.AnyAsync(c => c.Id == model.CategoryId))
            return OperationResult.Fail("La categoría seleccionada no existe.");

        // Reemplazo de imagen solo si se subió una nueva.
        if (model.Image is not null)
        {
            var saved = await _files.SaveProductImageAsync(model.Image);
            if (!saved.Succeeded)
                return OperationResult.Fail(saved.Error!);

            var oldImage = product.ImagePath;
            product.ImagePath = saved.RelativePath;
            _files.DeleteProductImage(oldImage);
        }

        // El SKU no se toca.
        product.Name = model.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        product.Price = model.Price;
        product.SalePrice = model.SalePrice;
        product.CategoryId = model.CategoryId;
        product.Stock = model.Stock;
        product.MinStock = model.MinStock;
        product.IsActive = model.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("[AUDIT] {User} actualizó el producto '{Name}' (SKU {Sku}, Id {Id}).",
            CurrentUser, product.Name, product.Sku, product.Id);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return OperationResult.Fail("El producto no existe.");

        // Borrado lógico: se conserva la imagen y el historial.
        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("[AUDIT] {User} eliminó (soft delete) el producto '{Name}' (SKU {Sku}, Id {Id}).",
            CurrentUser, product.Name, product.Sku, product.Id);
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
    }
}

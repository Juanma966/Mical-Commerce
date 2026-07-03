using Mical.Areas.Admin.Models;
using Mical.Data;
using Mical.Entities;
using Mical.Models;
using Mical.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Mical.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CategoryService> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public CategoryService(
        ApplicationDbContext db,
        ILogger<CategoryService> logger,
        IHttpContextAccessor httpContext)
    {
        _db = db;
        _logger = logger;
        _httpContext = httpContext;
    }

    private string CurrentUser => _httpContext.HttpContext?.User?.Identity?.Name ?? "sistema";

    public async Task<IReadOnlyList<AdminCategoryListItemVm>> GetAllForAdminAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new AdminCategoryListItemVm
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryFormVm?> GetForEditAsync(int id)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryFormVm
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AdminCategoryListItemVm?> GetByIdAsync(int id)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new AdminCategoryListItemVm
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<OperationResult> CreateAsync(CategoryFormVm model)
    {
        var name = model.Name.Trim();

        if (await NameExistsAsync(name, excludeId: null))
            return OperationResult.Fail("Ya existe una categoría con ese nombre.");

        var category = new Category
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        _logger.LogInformation("[AUDIT] {User} creó la categoría '{Name}' (Id {Id}).",
            CurrentUser, category.Name, category.Id);
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateAsync(CategoryFormVm model)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.Id);
        if (category is null)
            return OperationResult.Fail("La categoría no existe.");

        var name = model.Name.Trim();
        if (await NameExistsAsync(name, excludeId: model.Id))
            return OperationResult.Fail("Ya existe una categoría con ese nombre.");

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        category.IsActive = model.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("[AUDIT] {User} actualizó la categoría '{Name}' (Id {Id}).",
            CurrentUser, category.Name, category.Id);
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return OperationResult.Fail("La categoría no existe.");

        // Borrado lógico.
        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("[AUDIT] {User} eliminó (soft delete) la categoría '{Name}' (Id {Id}).",
            CurrentUser, category.Name, category.Id);
        return OperationResult.Success();
    }

    private Task<bool> NameExistsAsync(string name, int? excludeId)
    {
        var normalized = name.ToLower();
        return _db.Categories.AnyAsync(c =>
            c.Name.ToLower() == normalized && (excludeId == null || c.Id != excludeId));
    }
}

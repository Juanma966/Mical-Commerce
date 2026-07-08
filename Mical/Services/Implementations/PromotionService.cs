using Mical.Areas.Admin.Models;
using Mical.Data;
using Mical.Entities;
using Mical.Models;
using Mical.Services.Interfaces;
using Mical.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Mical.Services.Implementations;

public class PromotionService : IPromotionService
{
    private const string ImageSubfolder = "promotions";

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;

    public PromotionService(ApplicationDbContext db, IFileStorageService files)
    {
        _db = db;
        _files = files;
    }

    public async Task<IReadOnlyList<AdminPromotionListItemVm>> GetAllForAdminAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _db.Promotions
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Id)
            .Select(p => new AdminPromotionListItemVm
            {
                Id = p.Id,
                Title = p.Title,
                ImagePath = p.ImagePath,
                IsActive = p.IsActive,
                DisplayOrder = p.DisplayOrder,
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
                IsVisibleNow = p.IsActive
                    && (p.StartsAt == null || p.StartsAt <= today)
                    && (p.EndsAt == null || p.EndsAt >= today)
            })
            .ToListAsync();
    }

    public async Task<PromotionFormVm?> GetForEditAsync(int id)
    {
        return await _db.Promotions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PromotionFormVm
            {
                Id = p.Id,
                Title = p.Title,
                Subtitle = p.Subtitle,
                LinkUrl = p.LinkUrl,
                LinkText = p.LinkText,
                DisplayOrder = p.DisplayOrder,
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt,
                IsActive = p.IsActive,
                ExistingImagePath = p.ImagePath
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AdminPromotionListItemVm?> GetByIdAsync(int id)
    {
        return await _db.Promotions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new AdminPromotionListItemVm
            {
                Id = p.Id,
                Title = p.Title,
                ImagePath = p.ImagePath,
                IsActive = p.IsActive,
                DisplayOrder = p.DisplayOrder,
                StartsAt = p.StartsAt,
                EndsAt = p.EndsAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<OperationResult> CreateAsync(PromotionFormVm model)
    {
        if (!ValidDateRange(model))
            return OperationResult.Fail("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        string? imagePath = null;
        if (model.Image is not null)
        {
            var saved = await _files.SaveImageAsync(model.Image, ImageSubfolder);
            if (!saved.Succeeded)
                return OperationResult.Fail(saved.Error!);
            imagePath = saved.RelativePath;
        }

        var promo = new Promotion
        {
            Title = model.Title.Trim(),
            Subtitle = Clean(model.Subtitle),
            LinkUrl = Clean(model.LinkUrl),
            LinkText = Clean(model.LinkText),
            DisplayOrder = model.DisplayOrder,
            StartsAt = model.StartsAt,
            EndsAt = model.EndsAt,
            IsActive = model.IsActive,
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow
        };

        _db.Promotions.Add(promo);
        await _db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateAsync(PromotionFormVm model)
    {
        var promo = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (promo is null)
            return OperationResult.Fail("La promoción no existe.");

        if (!ValidDateRange(model))
            return OperationResult.Fail("La fecha 'Hasta' no puede ser anterior a 'Desde'.");

        if (model.Image is not null)
        {
            var saved = await _files.SaveImageAsync(model.Image, ImageSubfolder);
            if (!saved.Succeeded)
                return OperationResult.Fail(saved.Error!);

            var oldImage = promo.ImagePath;
            promo.ImagePath = saved.RelativePath;
            _files.DeleteImage(oldImage);
        }

        promo.Title = model.Title.Trim();
        promo.Subtitle = Clean(model.Subtitle);
        promo.LinkUrl = Clean(model.LinkUrl);
        promo.LinkText = Clean(model.LinkText);
        promo.DisplayOrder = model.DisplayOrder;
        promo.StartsAt = model.StartsAt;
        promo.EndsAt = model.EndsAt;
        promo.IsActive = model.IsActive;
        promo.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        var promo = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        if (promo is null)
            return OperationResult.Fail("La promoción no existe.");

        // Borrado lógico; la imagen sí se elimina físicamente (no hay historial que preservar).
        _files.DeleteImage(promo.ImagePath);
        promo.ImagePath = null;
        promo.IsDeleted = true;
        promo.IsActive = false;
        promo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<OperationResult> ToggleActiveAsync(int id)
    {
        var promo = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        if (promo is null)
            return OperationResult.Fail("La promoción no existe.");

        promo.IsActive = !promo.IsActive;
        promo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return OperationResult.Success();
    }

    public async Task<IReadOnlyList<PromotionBannerVm>> GetActiveBannersAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _db.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive
                        && (p.StartsAt == null || p.StartsAt <= today)
                        && (p.EndsAt == null || p.EndsAt >= today))
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.Id)
            .Select(p => new PromotionBannerVm
            {
                Title = p.Title,
                Subtitle = p.Subtitle,
                ImagePath = p.ImagePath,
                LinkUrl = p.LinkUrl,
                LinkText = p.LinkText
            })
            .ToListAsync();
    }

    private static bool ValidDateRange(PromotionFormVm model) =>
        model.StartsAt is null || model.EndsAt is null || model.EndsAt >= model.StartsAt;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

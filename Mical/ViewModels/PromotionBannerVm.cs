namespace Mical.ViewModels;

/// <summary>Banner de promoción para mostrar en la home (solo lectura).</summary>
public class PromotionBannerVm
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ImagePath { get; set; }
    public string? LinkUrl { get; set; }
    public string? LinkText { get; set; }
}

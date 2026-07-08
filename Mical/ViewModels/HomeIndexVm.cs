namespace Mical.ViewModels;

/// <summary>Datos de la home pública: banners de promoción y productos destacados.</summary>
public class HomeIndexVm
{
    public IReadOnlyList<PromotionBannerVm> Promotions { get; set; } = new List<PromotionBannerVm>();
    public IReadOnlyList<ProductCardVm> Featured { get; set; } = new List<ProductCardVm>();
}

namespace Mical.ViewModels;

/// <summary>Datos mínimos de un producto para el sitemap.</summary>
public class SitemapEntryVm
{
    public int Id { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>Contenido dinámico del sitemap: productos y categorías visibles.</summary>
public class SitemapDataVm
{
    public IReadOnlyList<SitemapEntryVm> Products { get; set; } = new List<SitemapEntryVm>();
    public IReadOnlyList<int> CategoryIds { get; set; } = new List<int>();
}

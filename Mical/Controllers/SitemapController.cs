using System.Text;
using System.Xml;
using Mical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>Genera dinámicamente el sitemap.xml con las URLs públicas indexables.</summary>
public class SitemapController : Controller
{
    private readonly ICatalogService _catalog;

    public SitemapController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    // GET: /sitemap.xml
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index()
    {
        var data = await _catalog.GetSitemapDataAsync();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };

        await using (var writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Páginas fijas.
            WriteUrl(writer, baseUrl + Url.Action("Index", "Home")!, null, "1.0");
            WriteUrl(writer, baseUrl + Url.Action("Index", "Shop")!, null, "0.8");

            // Filtros por categoría.
            foreach (var categoryId in data.CategoryIds)
            {
                var url = baseUrl + Url.Action("Index", "Shop", new { categoria = categoryId });
                WriteUrl(writer, url, null, "0.6");
            }

            // Detalle de cada producto visible.
            foreach (var p in data.Products)
            {
                var url = baseUrl + Url.Action("Details", "Product", new { id = p.Id });
                WriteUrl(writer, url, p.LastModified, "0.7");
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    private static void WriteUrl(XmlWriter writer, string loc, DateTime? lastMod, string priority)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", loc);
        if (lastMod is not null)
            writer.WriteElementString("lastmod", lastMod.Value.ToUniversalTime().ToString("yyyy-MM-dd"));
        writer.WriteElementString("priority", priority);
        writer.WriteEndElement();
    }
}

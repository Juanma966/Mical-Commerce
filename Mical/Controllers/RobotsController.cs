using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Mical.Controllers;

/// <summary>
/// Sirve robots.txt de forma dinámica para poder incluir la URL absoluta del
/// sitemap con el host real de cada entorno.
/// </summary>
public class RobotsController : Controller
{
    // GET: /robots.txt
    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Index()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /Account/");
        sb.AppendLine("Disallow: /Cart");
        sb.AppendLine("Disallow: /Checkout");
        sb.AppendLine("Disallow: /Order");
        sb.AppendLine("Disallow: /Admin/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}

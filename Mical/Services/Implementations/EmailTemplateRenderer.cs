using System.Text.Encodings.Web;
using Mical.Services.Interfaces;

namespace Mical.Services.Implementations;

/// <summary>
/// Renderiza plantillas HTML ubicadas en <c>EmailTemplates/</c> (bajo el content
/// root). Reemplaza los placeholders <c>{{Clave}}</c>, escapando los valores para
/// evitar inyección de HTML.
/// </summary>
public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private const string TemplatesFolder = "EmailTemplates";

    private readonly IWebHostEnvironment _env;

    public EmailTemplateRenderer(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> RenderAsync(string templateName, IReadOnlyDictionary<string, string> values)
    {
        // Evita traversal: nos quedamos solo con el nombre del archivo.
        var safeName = Path.GetFileName(templateName);
        var path = Path.Combine(_env.ContentRootPath, TemplatesFolder, $"{safeName}.html");

        if (!File.Exists(path))
            throw new FileNotFoundException($"No se encontró la plantilla de email '{safeName}'.", path);

        var html = await File.ReadAllTextAsync(path);

        foreach (var (key, value) in values)
            html = html.Replace($"{{{{{key}}}}}", HtmlEncoder.Default.Encode(value ?? string.Empty));

        return html;
    }
}

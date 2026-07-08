namespace Mical.Services.Interfaces;

/// <summary>
/// Carga plantillas HTML de <c>EmailTemplates/</c> y reemplaza sus placeholders
/// <c>{{Clave}}</c> por los valores provistos. Mantiene el HTML fuera del código.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <param name="templateName">Nombre del archivo sin extensión (ej. "ForgotPassword").</param>
    /// <param name="values">Placeholders a reemplazar (clave → valor).</param>
    Task<string> RenderAsync(string templateName, IReadOnlyDictionary<string, string> values);
}

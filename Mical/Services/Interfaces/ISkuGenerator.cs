namespace Mical.Services.Interfaces;

/// <summary>
/// Genera SKUs únicos y correlativos de producto. El valor es no editable y no
/// se deriva del Id. Formato: <c>PRD-2026-000123</c>.
/// </summary>
public interface ISkuGenerator
{
    Task<string> GenerateAsync();
}

namespace Mical.ViewModels;

/// <summary>Sugerencia liviana para el autocomplete del buscador.</summary>
public class ProductSuggestionVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public decimal Price { get; set; }
}

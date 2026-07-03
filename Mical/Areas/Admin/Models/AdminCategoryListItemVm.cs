namespace Mical.Areas.Admin.Models;

/// <summary>Fila del listado de categorías en el panel admin.</summary>
public class AdminCategoryListItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

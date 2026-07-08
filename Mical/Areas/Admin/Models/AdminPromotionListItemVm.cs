namespace Mical.Areas.Admin.Models;

/// <summary>Fila del listado de promociones en el panel admin.</summary>
public class AdminPromotionListItemVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateOnly? StartsAt { get; set; }
    public DateOnly? EndsAt { get; set; }

    /// <summary>Si hoy está dentro de la ventana de vigencia y está activa.</summary>
    public bool IsVisibleNow { get; set; }
}

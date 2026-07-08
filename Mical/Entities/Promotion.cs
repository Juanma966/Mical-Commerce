using Mical.Entities.Common;

namespace Mical.Entities;

/// <summary>
/// Banner de promoción que se muestra en la home. Se administra desde el panel.
/// Puede tener imagen y un enlace opcional, y una ventana de fechas de vigencia.
/// </summary>
public class Promotion : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    /// <summary>Ruta relativa de la imagen en wwwroot/uploads/promotions.</summary>
    public string? ImagePath { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Orden de aparición (menor primero).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Inicio de vigencia (opcional). Si es null, sin límite inferior.</summary>
    public DateOnly? StartsAt { get; set; }

    /// <summary>Fin de vigencia inclusivo (opcional). Si es null, sin límite superior.</summary>
    public DateOnly? EndsAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

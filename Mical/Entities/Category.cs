using Mical.Entities.Common;

namespace Mical.Entities;

/// <summary>
/// Categoría del catálogo (p. ej. Regalería, Imprenta). Raíz simple: agrupa
/// productos. Usa borrado lógico; al desactivarla, sus productos dejan de
/// listarse en el catálogo público.
/// </summary>
public class Category : IAuditable, ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>Nombre visible y único de la categoría.</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Ruta relativa de una imagen opcional (si la plantilla la usa).</summary>
    public string? ImagePath { get; set; }

    /// <summary>Si está inactiva, no se muestra en el catálogo público.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

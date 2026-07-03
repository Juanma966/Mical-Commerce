namespace Mical.Entities.Common;

/// <summary>
/// Entidad con marcas de tiempo de creación/actualización. La capa de Services
/// (y, más adelante, un interceptor) las completa automáticamente.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

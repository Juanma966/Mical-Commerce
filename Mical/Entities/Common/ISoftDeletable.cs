namespace Mical.Entities.Common;

/// <summary>
/// Entidad con borrado lógico. EF Core aplica un filtro global
/// <c>IsDeleted == false</c> para excluirlas de las consultas sin repetir Where.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

namespace Mical.Helpers;

/// <summary>
/// Nombres de los roles de la aplicación. Se usan como constantes para evitar
/// strings mágicos en atributos <c>[Authorize]</c>, seed y asignaciones.
/// </summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Usuario = "Usuario";

    /// <summary>Todos los roles conocidos (para el seed inicial).</summary>
    public static readonly string[] All = { Administrador, Usuario };
}

namespace Mical.Helpers;

/// <summary>
/// Nombres de las políticas de autorización. Se registran en <c>Program.cs</c>
/// y se referencian en <c>[Authorize(Policy = ...)]</c>.
/// </summary>
public static class Policies
{
    /// <summary>Requiere el rol Administrador. Protege todo el área Admin.</summary>
    public const string AdminOnly = "AdminOnly";
}

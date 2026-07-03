using Microsoft.AspNetCore.Identity;

namespace Mical.Entities;

/// <summary>
/// Usuario de la aplicación. Extiende <see cref="IdentityUser"/> (que ya aporta
/// email, hash de contraseña, lockout, logins externos, etc.) con los datos
/// propios del dominio Mical.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Nombre y apellido para mostrar (saludos, snapshot en pedidos).</summary>
    public string? FullName { get; set; }

    /// <summary>Fecha de alta del usuario (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace Mical.Services.Interfaces;

/// <summary>
/// Envío de emails desacoplado del proveedor. La aplicación solo depende de esta
/// abstracción; la implementación concreta (Resend hoy) puede cambiarse sin tocar
/// controllers ni otros servicios.
/// </summary>
public interface IEmailService
{
    /// <summary>Envía un email HTML a un destinatario.</summary>
    Task SendEmailAsync(string to, string subject, string html);
}

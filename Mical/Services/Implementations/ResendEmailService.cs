using Mical.Services.Interfaces;
using Resend;

namespace Mical.Services.Implementations;

/// <summary>
/// Implementación de <see cref="IEmailService"/> con el SDK oficial de Resend.
/// Es el único punto del sistema que conoce Resend.
/// </summary>
public class ResendEmailService : IEmailService
{
    private const string DefaultFrom = "Mical <onboarding@resend.dev>";

    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _from;

    public ResendEmailService(IResend resend, IConfiguration configuration, ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _logger = logger;
        _from = configuration["Resend:From"] ?? DefaultFrom;
    }

    public async Task SendEmailAsync(string to, string subject, string html)
    {
        var message = new EmailMessage
        {
            From = _from,
            To = to,
            Subject = subject,
            HtmlBody = html
        };

        try
        {
            var response = await _resend.EmailSendAsync(message);
            _logger.LogInformation(
                "Email enviado a {To} (asunto: {Subject}). Id: {EmailId}", to, subject, response.Content);
        }
        catch (ResendException ex)
        {
            // Error propio del proveedor (rechazo de API, dominio no verificado, etc.).
            _logger.LogError(ex, "Resend rechazó el envío a {To} (asunto: {Subject}).", to, subject);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar el email a {To} (asunto: {Subject}).", to, subject);
            throw;
        }
    }
}

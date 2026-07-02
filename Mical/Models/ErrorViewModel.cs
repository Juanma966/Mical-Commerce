namespace Mical.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>Código HTTP asociado al error (404, 500, etc.). 0 si no aplica.</summary>
    public int StatusCode { get; set; }

    /// <summary>Título amigable para mostrar al usuario.</summary>
    public string Title { get; set; } = "Algo salió mal";

    /// <summary>Mensaje amigable para mostrar al usuario.</summary>
    public string Message { get; set; } = "Ocurrió un error inesperado. Intentá nuevamente en unos minutos.";
}

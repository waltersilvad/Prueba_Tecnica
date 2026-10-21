namespace Facturacion.Application.Correo;

/// <summary>
/// Envía correos electrónicos. La implementación se elige por configuración (Consola o Brevo).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envía el mensaje. Si el envío no se completa, lanza una excepción: el llamador decide cómo aislar el fallo.
    /// </summary>
    Task EnviarAsync(MensajeCorreo mensaje, CancellationToken cancellationToken);
}

/// <summary>Correo listo para enviar.</summary>
public sealed record MensajeCorreo(
    string Destinatario,
    string NombreDestinatario,
    string Asunto,
    string CuerpoHtml,
    string CuerpoTexto);

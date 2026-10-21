using System.Net.Sockets;
using Facturacion.Application.Correo;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Facturacion.Infrastructure.Correo;

/// <summary>
/// Envío real por SMTP (Brevo) con STARTTLS y reintento simple ante fallas transitorias.
/// </summary>
internal sealed partial class BrevoSmtpEmailSender(
    IOptions<OpcionesEmail> opcionesEmail,
    IOptions<OpcionesSmtp> opcionesSmtp,
    ILogger<BrevoSmtpEmailSender> logger) : IEmailSender
{
    public async Task EnviarAsync(MensajeCorreo mensaje, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mensaje);

        var smtp = opcionesSmtp.Value;
        using var mime = ConstructorMensajeMime.Crear(mensaje, opcionesEmail.Value);

        for (var intento = 1; ; intento++)
        {
            try
            {
                await EnviarUnaVezAsync(mime, smtp, cancellationToken);
                return;
            }
            catch (Exception ex) when (EsTransitoria(ex) && intento <= smtp.Reintentos)
            {
                LogReintento(logger, ex, mensaje.Destinatario, intento, smtp.Reintentos);
                await Task.Delay(smtp.EsperaEntreReintentos, cancellationToken);
            }
        }
    }

    private static async Task EnviarUnaVezAsync(MimeMessage mime, OpcionesSmtp smtp, CancellationToken cancellationToken)
    {
        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(smtp.Servidor, smtp.Puerto, SecureSocketOptions.StartTls, cancellationToken);
        await cliente.AuthenticateAsync(smtp.Usuario, smtp.Clave, cancellationToken);
        await cliente.SendAsync(mime, cancellationToken);
        await cliente.DisconnectAsync(quit: true, cancellationToken);
    }

    /// <summary>
    /// Errores de red o de protocolo, y respuestas SMTP 4xx (rechazo temporal). Los 5xx y los errores
    /// de autenticación son permanentes: reintentarlos no sirve.
    /// </summary>
    /// <remarks>Es <see langword="internal"/> para probar la clasificación sin conectarse a ningún servidor SMTP.</remarks>
    internal static bool EsTransitoria(Exception ex) => ex switch
    {
        SmtpCommandException comando => (int)comando.StatusCode is >= 400 and < 500,
        AuthenticationException => false,
        SocketException or IOException or TimeoutException or SmtpProtocolException or ServiceNotConnectedException => true,
        _ => false,
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falla transitoria enviando a {Destinatario}; reintento {Intento} de {Maximo}.")]
    private static partial void LogReintento(ILogger logger, Exception ex, string destinatario, int intento, int maximo);
}

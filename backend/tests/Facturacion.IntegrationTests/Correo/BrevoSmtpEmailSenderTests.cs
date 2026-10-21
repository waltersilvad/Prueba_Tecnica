using System.Net.Sockets;
using Facturacion.Infrastructure.Correo;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace Facturacion.IntegrationTests.Correo;

/// <summary>
/// Clasificación de fallas del envío por SMTP. No se conecta a ningún servidor: solo decide qué se reintenta.
/// </summary>
public sealed class BrevoSmtpEmailSenderTests
{
    public static TheoryData<Exception> FallasTransitorias =>
    [
        new SocketException((int)SocketError.ConnectionRefused),
        new IOException("Conexión interrumpida."),
        new TimeoutException(),
        new SmtpProtocolException("Respuesta inesperada del servidor."),
        new ServiceNotConnectedException(),
        new SmtpCommandException(SmtpErrorCode.MessageNotAccepted, SmtpStatusCode.MailboxBusy, "450 Buzón ocupado."),
    ];

    public static TheoryData<Exception> FallasPermanentes =>
    [
        new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, SmtpStatusCode.MailboxUnavailable, "550 Buzón no existe."),
        new AuthenticationException("Credenciales inválidas."),
        new InvalidOperationException("Error de programación."),
    ];

    [Theory]
    [MemberData(nameof(FallasTransitorias))]
    public void EsTransitoria_ConFallaDeRedOProtocoloORespuesta4xx_SeReintenta(Exception falla) =>
        Assert.True(BrevoSmtpEmailSender.EsTransitoria(falla));

    [Theory]
    [MemberData(nameof(FallasPermanentes))]
    public void EsTransitoria_ConRespuesta5xxAutenticacionUOtroError_NoSeReintenta(Exception falla) =>
        Assert.False(BrevoSmtpEmailSender.EsTransitoria(falla));
}

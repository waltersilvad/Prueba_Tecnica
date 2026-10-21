using Facturacion.Application.Correo;
using MimeKit;

namespace Facturacion.Infrastructure.Correo;

/// <summary>
/// Construye el mismo mensaje MIME (HTML + texto plano) para cualquier proveedor.
/// </summary>
internal static class ConstructorMensajeMime
{
    public static MimeMessage Crear(MensajeCorreo mensaje, OpcionesEmail opciones)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(opciones.NombreRemitente, opciones.Remitente));
        mime.To.Add(new MailboxAddress(mensaje.NombreDestinatario, mensaje.Destinatario));
        mime.Subject = mensaje.Asunto;
        mime.Body = new BodyBuilder
        {
            HtmlBody = mensaje.CuerpoHtml,
            TextBody = mensaje.CuerpoTexto,
        }.ToMessageBody();

        return mime;
    }
}

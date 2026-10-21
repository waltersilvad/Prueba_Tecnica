using System.Globalization;
using Facturacion.Application.Comun;
using Facturacion.Application.Correo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Facturacion.Infrastructure.Correo;

/// <summary>
/// Proveedor por defecto: no envía nada. Guarda cada correo como <c>.eml</c> (se abre con doble clic en
/// cualquier cliente de correo) y lo deja en el log. Permite probar el flujo sin credenciales.
/// </summary>
internal sealed partial class ConsolaEmailSender(
    IOptions<OpcionesEmail> opciones,
    IReloj reloj,
    ILogger<ConsolaEmailSender> logger) : IEmailSender
{
    public async Task EnviarAsync(MensajeCorreo mensaje, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mensaje);

        var carpeta = Path.GetFullPath(opciones.Value.CarpetaSalida);
        Directory.CreateDirectory(carpeta);

        var archivo = Path.Combine(carpeta, NombreArchivo(mensaje.Destinatario));
        using var mime = ConstructorMensajeMime.Crear(mensaje, opciones.Value);
        await mime.WriteToAsync(archivo, cancellationToken);

        LogCorreoGuardado(logger, mensaje.Destinatario, mensaje.Asunto, archivo);
    }

    private string NombreArchivo(string destinatario)
    {
        var seguro = string.Concat(destinatario.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '@' ? c : '_'));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{reloj.AhoraUtc:yyyyMMdd-HHmmssfff}_{seguro}_{Guid.NewGuid():N}.eml");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Correo simulado para {Destinatario} «{Asunto}» guardado en {Archivo}.")]
    private static partial void LogCorreoGuardado(ILogger logger, string destinatario, string asunto, string archivo);
}

using Facturacion.Infrastructure.Correo;

namespace Facturacion.Api.Configuracion;

/// <summary>
/// Mensajes de log del arranque de la API.
/// </summary>
internal static partial class RegistroArranque
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Proveedor de correo: {Proveedor}. Consola guarda los correos como .eml; Brevo los envía de verdad.")]
    public static partial void ProveedorCorreo(ILogger logger, ProveedorEmail proveedor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Configuración local cargada desde .env: {Cantidad} variables.")]
    public static partial void ArchivoEnvCargado(ILogger logger, int cantidad);
}

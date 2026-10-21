using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Envios;

/// <summary>Resultado de un intento de envío.</summary>
public enum ResultadoEnvio
{
    Enviado,
    Fallido,
}

/// <summary>
/// Intento de envío de un aviso. <see cref="EjecucionId"/> agrupa los envíos de una misma ejecución.
/// </summary>
public sealed record RegistroEnvio(
    string EjecucionId,
    string ClienteId,
    string FacturaId,
    TipoAviso TipoAviso,
    string Destinatario,
    DateTime Fecha,
    ResultadoEnvio Resultado,
    string? Error);

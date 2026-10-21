namespace Facturacion.Application.Envios;

/// <summary>
/// Registro del historial de envíos expuesto por la API.
/// </summary>
public sealed record EnvioDto(
    string EjecucionId,
    string ClienteId,
    string FacturaId,
    string TipoAviso,
    string Destinatario,
    DateTime Fecha,
    string Resultado,
    string? Error)
{
    public static EnvioDto Desde(RegistroEnvio registro)
    {
        ArgumentNullException.ThrowIfNull(registro);
        return new EnvioDto(
            registro.EjecucionId,
            registro.ClienteId,
            registro.FacturaId,
            registro.TipoAviso.ToString(),
            registro.Destinatario,
            registro.Fecha,
            registro.Resultado.ToString(),
            registro.Error);
    }
}

namespace Facturacion.Application.Envios;

/// <summary>
/// Consulta del historial de envíos.
/// </summary>
public sealed class ConsultarEnviosUseCase(IEnvioRepository envios)
{
    /// <summary>Devuelve el historial, del más reciente al más antiguo, opcionalmente de un cliente.</summary>
    public async Task<IReadOnlyList<EnvioDto>> EjecutarAsync(string? clienteId, CancellationToken cancellationToken)
    {
        var registros = await envios.ObtenerAsync(clienteId, cancellationToken);
        return [.. registros.Select(EnvioDto.Desde)];
    }
}

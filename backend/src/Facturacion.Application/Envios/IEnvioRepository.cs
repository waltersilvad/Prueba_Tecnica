namespace Facturacion.Application.Envios;

/// <summary>
/// Historial de envíos de avisos (auditoría).
/// </summary>
public interface IEnvioRepository
{
    /// <summary>Registra un intento de envío, exitoso o fallido.</summary>
    Task RegistrarAsync(RegistroEnvio registro, CancellationToken cancellationToken);

    /// <summary>Elimina el historial de las facturas indicadas.</summary>
    /// <returns>Cantidad de registros eliminados.</returns>
    Task<long> EliminarPorFacturasAsync(IReadOnlyCollection<string> facturaIds, CancellationToken cancellationToken);

    /// <summary>Elimina el historial de un cliente.</summary>
    /// <returns>Cantidad de registros eliminados.</returns>
    Task<long> EliminarPorClienteAsync(string clienteId, CancellationToken cancellationToken);

    /// <summary>Obtiene el historial, del más reciente al más antiguo, opcionalmente de un solo cliente.</summary>
    Task<IReadOnlyList<RegistroEnvio>> ObtenerAsync(string? clienteId, CancellationToken cancellationToken);
}

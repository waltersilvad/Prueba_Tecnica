using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Facturas;

/// <summary>
/// Persistencia de facturas.
/// </summary>
public interface IFacturaRepository
{
    /// <summary>
    /// Busca facturas según el filtro. Los documentos que no se pueden convertir en una
    /// <see cref="Factura"/> válida (por ejemplo, con un estado desconocido) se devuelven aparte.
    /// </summary>
    Task<LecturaFacturas> BuscarAsync(FiltroFacturas filtro, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda una factura nueva. El número de factura es único.
    /// </summary>
    /// <returns><see langword="false"/> si ya existe una factura con el mismo número.</returns>
    Task<bool> IntentarAgregarAsync(Factura factura, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda el estado y la fecha de actualización de la factura <b>solo si</b> en la base sigue en
    /// <paramref name="estadoEsperado"/>. Evita pisar cambios de otra ejecución.
    /// </summary>
    /// <returns><see langword="true"/> si se modificó la factura; <see langword="false"/> si hubo conflicto.</returns>
    Task<bool> ActualizarEstadoAsync(Factura factura, EstadoFactura estadoEsperado, CancellationToken cancellationToken);

    /// <summary>Elimina las facturas indicadas.</summary>
    /// <returns>Cantidad de facturas eliminadas.</returns>
    Task<long> EliminarAsync(IReadOnlyCollection<string> facturaIds, CancellationToken cancellationToken);

    /// <summary>Elimina todas las facturas de un cliente.</summary>
    /// <returns>Cantidad de facturas eliminadas.</returns>
    Task<long> EliminarPorClienteAsync(string clienteId, CancellationToken cancellationToken);

    /// <summary>
    /// Cantidad y suma de montos agrupadas por cliente, estado y moneda.
    /// </summary>
    Task<IReadOnlyList<ConteoFacturas>> ContarPorClienteAsync(CancellationToken cancellationToken);
}

using Facturacion.Application.Comun;
using Facturacion.Application.Envios;
using Facturacion.Domain.Comun;

namespace Facturacion.Application.Facturas;

/// <summary>Qué se eliminó junto con las facturas.</summary>
public sealed record ResultadoEliminacionFacturas(
    IReadOnlyList<string> Numeros,
    long FacturasEliminadas,
    long EnviosEliminados);

/// <summary>
/// Elimina una o varias facturas junto con su historial de envíos.
/// </summary>
/// <remarks>
/// Toma el mismo bloqueo que el proceso de avisos (no se borra una factura a mitad de su envío). Valida toda la
/// selección antes de borrar: si alguna factura no existe, no se elimina ninguna. Borra primero los envíos y luego
/// las facturas, así un fallo a mitad no deja historial huérfano y la operación se puede repetir.
/// </remarks>
public sealed class EliminarFacturasUseCase(
    IFacturaRepository facturas,
    IEnvioRepository envios,
    IBloqueoEjecucion bloqueo)
{
    /// <summary>Elimina las facturas indicadas, o falla si la selección está vacía, alguna no existe o hay un proceso en curso.</summary>
    public async Task<Result<ResultadoEliminacionFacturas>> EjecutarAsync(
        IReadOnlyCollection<string> facturaIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(facturaIds);

        var ids = facturaIds.Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0)
        {
            return Result.Fallo<ResultadoEliminacionFacturas>(ErroresAplicacion.SeleccionVacia);
        }

        using var bloqueoAdquirido = bloqueo.IntentarAdquirir();
        if (bloqueoAdquirido is null)
        {
            return Result.Fallo<ResultadoEliminacionFacturas>(ErroresAplicacion.EjecucionEnCurso);
        }

        // También se pueden eliminar documentos que no cumplen el dominio (por ejemplo, con un estado desconocido).
        var lectura = await facturas.BuscarAsync(new FiltroFacturas(FacturaIds: ids), cancellationToken);
        var numerosPorId = lectura.Facturas.Select(f => (f.Id, f.Numero))
            .Concat(lectura.NoInterpretables.Select(f => (f.Id, f.Numero)))
            .ToDictionary(f => f.Id, f => f.Numero, StringComparer.Ordinal);

        if (ids.FirstOrDefault(id => !numerosPorId.ContainsKey(id)) is { } faltante)
        {
            return Result.Fallo<ResultadoEliminacionFacturas>(ErroresAplicacion.FacturaNoEncontrada(faltante));
        }

        var enviosEliminados = await envios.EliminarPorFacturasAsync(ids, cancellationToken);
        var facturasEliminadas = await facturas.EliminarAsync(ids, cancellationToken);

        return Result.Exito(new ResultadoEliminacionFacturas(
            [.. numerosPorId.Values.Order(StringComparer.Ordinal)], facturasEliminadas, enviosEliminados));
    }
}

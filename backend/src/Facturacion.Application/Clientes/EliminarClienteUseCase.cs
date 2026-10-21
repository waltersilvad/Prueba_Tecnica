using Facturacion.Application.Comun;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Comun;

namespace Facturacion.Application.Clientes;

/// <summary>Qué se eliminó junto con el cliente.</summary>
public sealed record ResultadoEliminacionCliente(
    string ClienteId,
    string Nombre,
    long FacturasEliminadas,
    long EnviosEliminados);

/// <summary>
/// Elimina un cliente junto con sus facturas y su historial de envíos.
/// </summary>
/// <remarks>
/// Toma el mismo bloqueo que el proceso de avisos para no borrar facturas a mitad de un envío. Borra primero lo que
/// depende del cliente (envíos y facturas) y al final el cliente: si algo falla a mitad, el cliente sigue existiendo
/// y la operación se puede repetir sin dejar registros huérfanos.
/// </remarks>
public sealed class EliminarClienteUseCase(
    IClienteRepository clientes,
    IFacturaRepository facturas,
    IEnvioRepository envios,
    IBloqueoEjecucion bloqueo)
{
    /// <summary>Elimina el cliente y lo que depende de él, o falla si no existe o hay un proceso en curso.</summary>
    public async Task<Result<ResultadoEliminacionCliente>> EjecutarAsync(string id, CancellationToken cancellationToken)
    {
        using var bloqueoAdquirido = bloqueo.IntentarAdquirir();
        if (bloqueoAdquirido is null)
        {
            return Result.Fallo<ResultadoEliminacionCliente>(ErroresAplicacion.EjecucionEnCurso);
        }

        var cliente = await clientes.ObtenerPorIdAsync(id, cancellationToken);
        if (cliente is null)
        {
            return Result.Fallo<ResultadoEliminacionCliente>(ErroresAplicacion.ClienteNoEncontrado(id));
        }

        var enviosEliminados = await envios.EliminarPorClienteAsync(cliente.Id, cancellationToken);
        var facturasEliminadas = await facturas.EliminarPorClienteAsync(cliente.Id, cancellationToken);
        await clientes.EliminarAsync(cliente.Id, cancellationToken);

        return Result.Exito(
            new ResultadoEliminacionCliente(cliente.Id, cliente.Nombre, facturasEliminadas, enviosEliminados));
    }
}

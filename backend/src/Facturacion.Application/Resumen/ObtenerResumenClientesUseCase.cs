using Facturacion.Application.Clientes;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Resumen;

/// <summary>
/// Resumen de cartera por cliente: conteo por estado, total y monto pendiente por moneda.
/// </summary>
public sealed class ObtenerResumenClientesUseCase(IClienteRepository clientes, IFacturaRepository facturas)
{
    /// <summary>
    /// Devuelve un resumen por cada cliente, incluidos los que no tienen facturas.
    /// El monto pendiente suma solo las facturas en primer o segundo recordatorio.
    /// </summary>
    public async Task<IReadOnlyList<ResumenClienteDto>> EjecutarAsync(CancellationToken cancellationToken)
    {
        var todos = await clientes.ObtenerTodosAsync(cancellationToken);
        var conteosPorCliente = (await facturas.ContarPorClienteAsync(cancellationToken))
            .ToLookup(c => c.ClienteId);

        return
        [
            .. todos.Select(cliente =>
            {
                var conteos = conteosPorCliente[cliente.Id].ToList();
                return new ResumenClienteDto(
                    cliente.Id,
                    cliente.Nombre,
                    cliente.Email,
                    Contar(conteos, EstadoFactura.PrimerRecordatorio),
                    Contar(conteos, EstadoFactura.SegundoRecordatorio),
                    Contar(conteos, EstadoFactura.Desactivado),
                    conteos.Sum(c => c.Cantidad),
                    SumarPendientePorMoneda(conteos));
            }),
        ];
    }

    private static int Contar(IEnumerable<ConteoFacturas> conteos, EstadoFactura estado) =>
        conteos.Where(c => c.Estado == estado).Sum(c => c.Cantidad);

    private static IReadOnlyList<MontoPorMonedaDto> SumarPendientePorMoneda(IEnumerable<ConteoFacturas> conteos) =>
    [
        .. conteos
            .Where(c => c.Estado != EstadoFactura.Desactivado)
            .GroupBy(c => c.Moneda, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new MontoPorMonedaDto(g.Key, g.Sum(c => c.MontoTotal))),
    ];
}

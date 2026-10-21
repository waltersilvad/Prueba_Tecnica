using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Facturas;

/// <summary>
/// Consulta de facturas con filtros opcionales por estado y cliente.
/// </summary>
public sealed class ConsultarFacturasUseCase(IFacturaRepository facturas, IClienteRepository clientes)
{
    /// <summary>
    /// Devuelve las facturas ordenadas por número. <paramref name="estado"/> usa el texto exacto del negocio;
    /// un estado desconocido falla con <see cref="ErroresAplicacion.CodigoFiltroInvalido"/>.
    /// </summary>
    public async Task<Result<IReadOnlyList<FacturaDto>>> EjecutarAsync(
        string? estado, string? clienteId, CancellationToken cancellationToken)
    {
        var filtro = CrearFiltro(estado, clienteId);
        if (filtro.EsFallido)
        {
            return Result.Fallo<IReadOnlyList<FacturaDto>>(filtro.Error);
        }

        var lectura = await facturas.BuscarAsync(filtro.Valor, cancellationToken);
        var nombres = (await clientes.ObtenerTodosAsync(cancellationToken)).ToDictionary(c => c.Id, c => c.Nombre);

        IReadOnlyList<FacturaDto> resultado =
        [
            .. lectura.Facturas
                .OrderBy(f => f.Numero, StringComparer.Ordinal)
                .Select(f => FacturaDto.Desde(f, nombres.GetValueOrDefault(f.ClienteId, string.Empty))),
        ];
        return Result.Exito(resultado);
    }

    private static Result<FiltroFacturas> CrearFiltro(string? estado, string? clienteId)
    {
        var clienteFiltro = string.IsNullOrWhiteSpace(clienteId) ? null : clienteId;
        if (string.IsNullOrWhiteSpace(estado))
        {
            return Result.Exito(new FiltroFacturas(clienteFiltro));
        }

        var estadoFiltro = EstadoFacturaTextos.DesdeTexto(estado);
        return estadoFiltro.EsExitoso
            ? Result.Exito(new FiltroFacturas(clienteFiltro, estadoFiltro.Valor))
            : Result.Fallo<FiltroFacturas>(ErroresAplicacion.FiltroInvalido(estadoFiltro.Error.Mensaje));
    }
}

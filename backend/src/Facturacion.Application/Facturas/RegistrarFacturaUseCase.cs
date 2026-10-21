using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Facturas;

/// <summary>
/// Datos de una factura nueva. Las fechas son de calendario (sin hora): se interpretan en la zona del negocio.
/// </summary>
public sealed record RegistrarFacturaComando(
    string ClienteId,
    string Numero,
    decimal Monto,
    string Moneda,
    DateOnly FechaEmision,
    DateOnly FechaVencimiento);

/// <summary>
/// Registra una factura nueva para un cliente existente. Siempre empieza en <c>primerrecordatorio</c>
/// (lo garantiza <see cref="Factura.Registrar"/>) y su número debe ser único.
/// </summary>
public sealed class RegistrarFacturaUseCase(
    IFacturaRepository facturas,
    IClienteRepository clientes,
    IGeneradorId generadorId,
    IReloj reloj,
    CalendarioNegocio calendario)
{
    /// <summary>
    /// Guarda la factura. Falla si el cliente no existe, si los datos no son válidos o si el número ya existe.
    /// </summary>
    public async Task<Result<FacturaDto>> EjecutarAsync(RegistrarFacturaComando comando, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var cliente = await clientes.ObtenerPorIdAsync(comando.ClienteId, cancellationToken);
        if (cliente is null)
        {
            return Result.Fallo<FacturaDto>(ErroresAplicacion.ClienteNoEncontrado(comando.ClienteId));
        }

        var factura = Factura.Registrar(
            generadorId.NuevoId(),
            comando.Numero,
            cliente.Id,
            comando.Monto,
            comando.Moneda,
            calendario.InicioDelDiaUtc(comando.FechaEmision),
            calendario.InicioDelDiaUtc(comando.FechaVencimiento),
            reloj.AhoraUtc);
        if (factura.EsFallido)
        {
            return Result.Fallo<FacturaDto>(factura.Error);
        }

        // La unicidad del número la garantiza la base (índice único): así no hay carrera entre comprobar y guardar.
        if (!await facturas.IntentarAgregarAsync(factura.Valor, cancellationToken))
        {
            return Result.Fallo<FacturaDto>(ErroresAplicacion.NumeroFacturaDuplicado(factura.Valor.Numero));
        }

        return Result.Exito(FacturaDto.Desde(factura.Valor, cliente.Nombre));
    }
}

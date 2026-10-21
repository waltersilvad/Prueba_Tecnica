using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Facturas;

/// <summary>
/// Datos de una factura expuestos por la API. <see cref="Estado"/> usa el texto exacto del negocio.
/// </summary>
public sealed record FacturaDto(
    string Id,
    string Numero,
    string ClienteId,
    string NombreCliente,
    decimal Monto,
    string Moneda,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    string Estado,
    DateTime FechaActualizacion)
{
    public static FacturaDto Desde(Factura factura, string nombreCliente)
    {
        ArgumentNullException.ThrowIfNull(factura);
        return new FacturaDto(
            factura.Id,
            factura.Numero,
            factura.ClienteId,
            nombreCliente,
            factura.Monto,
            factura.Moneda,
            factura.FechaEmision,
            factura.FechaVencimiento,
            factura.Estado.ATexto(),
            factura.FechaActualizacion);
    }
}

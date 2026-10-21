using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;

namespace Facturacion.Domain.Tests.Facturas;

/// <summary>
/// Construye facturas válidas por defecto para que cada prueba cambie solo el dato que le interesa.
/// </summary>
internal static class FacturaPrueba
{
    public static readonly DateTime FechaEmision = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime FechaVencimiento = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    public static Result<Factura> Crear(
        string id = "factura-1",
        string numero = "FAC-0001",
        string clienteId = "cliente-1",
        decimal monto = 150_000.50m,
        string moneda = "COP",
        DateTime? fechaEmision = null,
        DateTime? fechaVencimiento = null,
        EstadoFactura estado = EstadoFactura.PrimerRecordatorio,
        DateTime? fechaActualizacion = null) =>
        Factura.Crear(
            id,
            numero,
            clienteId,
            monto,
            moneda,
            fechaEmision ?? FechaEmision,
            fechaVencimiento ?? FechaVencimiento,
            estado,
            fechaActualizacion ?? FechaEmision);

    public static Factura CrearValida(EstadoFactura estado = EstadoFactura.PrimerRecordatorio) =>
        Crear(estado: estado).Valor;
}

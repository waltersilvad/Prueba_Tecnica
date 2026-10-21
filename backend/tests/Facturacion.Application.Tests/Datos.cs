using Facturacion.Domain.Clientes;
using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Tests;

/// <summary>
/// Datos de prueba válidos por defecto.
/// </summary>
internal static class Datos
{
    public static readonly DateTime Ahora = new(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc);

    /// <summary>Medianoche en Bogotá (UTC-5), para que la fecha local coincida con la UTC.</summary>
    public static readonly DateTime FechaEmision = new(2026, 9, 1, 5, 0, 0, DateTimeKind.Utc);

    public static Cliente CrearCliente(string id = "cliente-1", string nombre = "Comercial Andina", string? email = null) =>
        Cliente.Crear(id, nombre, email ?? $"{id}@example.com", FechaEmision).Valor;

    public static Factura CrearFactura(
        string numero,
        EstadoFactura estado = EstadoFactura.PrimerRecordatorio,
        string clienteId = "cliente-1",
        decimal monto = 100_000m,
        string moneda = "COP",
        DateTime? fechaVencimiento = null) =>
        Factura.Crear(
            $"id-{numero}",
            numero,
            clienteId,
            monto,
            moneda,
            FechaEmision,
            fechaVencimiento ?? FechaEmision.AddDays(30),
            estado,
            FechaEmision).Valor;
}

using System.Globalization;
using Facturacion.Domain.Facturas;
using MongoDB.Bson;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Datos de prueba: 3 clientes con facturas en distintos estados. Los ids son fijos para que, cada vez que se cargan
/// los datos de ejemplo, sigan sirviendo los ids de los ejemplos de Scalar (por ejemplo, <c>6716a1000000000000000001</c>).
/// </summary>
internal static class DatosSemilla
{
    /// <summary>Moneda de las facturas de prueba.</summary>
    public const string Moneda = "COP";

    public static readonly IReadOnlyList<ClienteSemilla> Clientes =
    [
        new(IdCliente(1), "Comercial Andina S.A.S.", "cartera.andina@example.com"),
        new(IdCliente(2), "Distribuidora del Sur Ltda.", "pagos.sur@example.com"),
        new(IdCliente(3), "Tecnología Caribe S.A.S.", "tesoreria.caribe@example.com"),
    ];

    public static readonly IReadOnlyList<FacturaSemilla> Facturas =
    [
        // Cliente 1: 2 en primer recordatorio, 1 en segundo, 1 desactivada.
        new(1001, 1, 1_250_000.00m, "2026-09-01", "2026-10-01", EstadoFactura.PrimerRecordatorio),
        new(1002, 1, 845_500.50m, "2026-09-10", "2026-10-10", EstadoFactura.PrimerRecordatorio),
        new(1003, 1, 2_300_000.00m, "2026-08-15", "2026-09-15", EstadoFactura.SegundoRecordatorio),
        new(1004, 1, 420_000.00m, "2026-07-01", "2026-08-01", EstadoFactura.Desactivado),

        // Cliente 2: 1 en primer recordatorio, 2 en segundo.
        new(2001, 2, 980_000.00m, "2026-09-05", "2026-10-05", EstadoFactura.PrimerRecordatorio),
        new(2002, 2, 1_575_250.75m, "2026-08-20", "2026-09-20", EstadoFactura.SegundoRecordatorio),
        new(2003, 2, 310_000.00m, "2026-08-25", "2026-09-25", EstadoFactura.SegundoRecordatorio),

        // Cliente 3: 3 en primer recordatorio.
        new(3001, 3, 3_450_000.00m, "2026-09-15", "2026-10-15", EstadoFactura.PrimerRecordatorio),
        new(3002, 3, 125_990.00m, "2026-09-18", "2026-10-18", EstadoFactura.PrimerRecordatorio),
        new(3003, 3, 760_000.00m, "2026-09-20", "2026-10-20", EstadoFactura.PrimerRecordatorio),
    ];

    private static ObjectId IdCliente(int numero) => new(string.Create(CultureInfo.InvariantCulture, $"6716a1{numero:D18}"));

    internal sealed record ClienteSemilla(ObjectId Id, string Nombre, string Email);

    internal sealed record FacturaSemilla(
        int Consecutivo, int Cliente, decimal Monto, string Emision, string Vencimiento, EstadoFactura Estado)
    {
        public ObjectId Id => new(string.Create(CultureInfo.InvariantCulture, $"6716a2{Consecutivo:D18}"));

        public ObjectId ClienteId => IdCliente(Cliente);

        public string Numero => string.Create(CultureInfo.InvariantCulture, $"FAC-{Consecutivo}");

        public DateTime FechaEmision => MedianocheEnBogota(Emision);

        public DateTime FechaVencimiento => MedianocheEnBogota(Vencimiento);

        /// <summary>
        /// 00:00 en Bogotá (UTC-5, sin horario de verano) son las 05:00 UTC; así la fecha mostrada
        /// en Bogotá coincide con la fecha del calendario.
        /// </summary>
        private static DateTime MedianocheEnBogota(string fecha) =>
            DateTime.SpecifyKind(
                DateTime.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddHours(5),
                DateTimeKind.Utc);
    }
}

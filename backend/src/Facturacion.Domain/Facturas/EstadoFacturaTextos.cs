using Facturacion.Domain.Comun;

namespace Facturacion.Domain.Facturas;

/// <summary>
/// Mapeo explícito entre <see cref="EstadoFactura"/> y los textos exactos que define el negocio
/// (y que se guardan en la base de datos).
/// </summary>
public static class EstadoFacturaTextos
{
    public const string PrimerRecordatorio = "primerrecordatorio";
    public const string SegundoRecordatorio = "segundorecordatorio";
    public const string Desactivado = "desactivado";

    /// <summary>Convierte el estado a su texto de negocio.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Si el valor del enum no está definido.</exception>
    public static string ATexto(this EstadoFactura estado) => estado switch
    {
        EstadoFactura.PrimerRecordatorio => PrimerRecordatorio,
        EstadoFactura.SegundoRecordatorio => SegundoRecordatorio,
        EstadoFactura.Desactivado => Desactivado,
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, "Estado de factura no definido."),
    };

    /// <summary>
    /// Convierte un texto de negocio en estado. La comparación es exacta (sensible a mayúsculas).
    /// Un texto desconocido devuelve un resultado fallido, no una excepción.
    /// </summary>
    public static Result<EstadoFactura> DesdeTexto(string? texto) => texto switch
    {
        PrimerRecordatorio => Result.Exito(EstadoFactura.PrimerRecordatorio),
        SegundoRecordatorio => Result.Exito(EstadoFactura.SegundoRecordatorio),
        Desactivado => Result.Exito(EstadoFactura.Desactivado),
        _ => Result.Fallo<EstadoFactura>(ErroresFactura.EstadoDesconocido(texto)),
    };
}

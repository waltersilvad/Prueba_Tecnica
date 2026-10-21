using Facturacion.Domain.Comun;

namespace Facturacion.Domain.Facturas;

/// <summary>
/// Catálogo de errores esperados de la entidad <see cref="Factura"/>.
/// </summary>
public static class ErroresFactura
{
    public static readonly ErrorDominio MontoNoPositivo =
        new("Factura.MontoNoPositivo", "El monto de la factura debe ser mayor que cero.", TipoError.Validacion);

    public static readonly ErrorDominio MonedaInvalida =
        new(
            "Factura.MonedaInvalida",
            "La moneda debe ser un código ISO 4217 de 3 letras en mayúsculas (por ejemplo, COP).",
            TipoError.Validacion);

    public static readonly ErrorDominio VencimientoAnteriorAEmision =
        new(
            "Factura.VencimientoAnteriorAEmision",
            "La fecha de vencimiento no puede ser anterior a la fecha de emisión.",
            TipoError.Validacion);

    public static readonly ErrorDominio EstadoInvalido =
        new("Factura.EstadoInvalido", "El estado de la factura no es válido.", TipoError.Validacion);

    /// <summary>Es un conflicto: la petición es correcta, pero el estado actual de la factura no la admite.</summary>
    public static readonly ErrorDominio EstadoFinal =
        new(
            "Factura.EstadoFinal",
            "La factura está desactivada: no requiere más avisos ni cambios de estado.",
            TipoError.Conflicto);

    public static ErrorDominio CampoRequerido(string campo) =>
        new("Factura.CampoRequerido", $"El campo '{campo}' de la factura es obligatorio.", TipoError.Validacion);

    public static ErrorDominio EstadoDesconocido(string? texto) =>
        new("Factura.EstadoDesconocido", $"El estado '{texto}' no es un estado de factura conocido.", TipoError.Validacion);
}

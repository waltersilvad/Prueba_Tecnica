using Facturacion.Domain.Comun;

namespace Facturacion.Application.Comun;

/// <summary>
/// Errores esperados de los casos de uso. La API los traduce a códigos HTTP por su <see cref="ErrorDominio.Tipo"/>;
/// los códigos son estables y viajan en la respuesta para que el cliente pueda distinguirlos.
/// </summary>
public static class ErroresAplicacion
{
    public const string CodigoClienteNoEncontrado = "Cliente.NoEncontrado";
    public const string CodigoEjecucionEnCurso = "Recordatorios.EjecucionEnCurso";
    public const string CodigoFiltroInvalido = "Consulta.FiltroInvalido";
    public const string CodigoFacturaNoEncontrada = "Factura.NoEncontrada";
    public const string CodigoNumeroFacturaDuplicado = "Factura.NumeroDuplicado";
    public const string CodigoSeleccionVacia = "Facturas.SeleccionVacia";

    public static ErrorDominio FacturaNoEncontrada(string facturaId) =>
        new(CodigoFacturaNoEncontrada, $"No existe una factura con id '{facturaId}'.", TipoError.NoEncontrado);

    public static readonly ErrorDominio SeleccionVacia =
        new(CodigoSeleccionVacia, "Selecciona al menos una factura.", TipoError.Validacion);

    public static ErrorDominio NumeroFacturaDuplicado(string numero) =>
        new(CodigoNumeroFacturaDuplicado, $"Ya existe una factura con el número '{numero}'.", TipoError.Conflicto);

    public static readonly ErrorDominio EjecucionEnCurso =
        new(
            CodigoEjecucionEnCurso,
            "Ya hay un proceso de recordatorios en ejecución. Intenta de nuevo en unos segundos.",
            TipoError.Conflicto);

    public static ErrorDominio ClienteNoEncontrado(string clienteId) =>
        new(CodigoClienteNoEncontrado, $"No existe un cliente con id '{clienteId}'.", TipoError.NoEncontrado);

    public static ErrorDominio FiltroInvalido(string detalle) =>
        new(CodigoFiltroInvalido, detalle, TipoError.Validacion);
}

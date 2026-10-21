using Facturacion.Application.Comun;
using Facturacion.Domain.Comun;

namespace Facturacion.Application.Tests.Comun;

/// <summary>
/// Cada error de los casos de uso declara su tipo: la API lo traduce a 400, 404 o 409 sin conocer los códigos.
/// </summary>
public sealed class ErroresAplicacionTests
{
    public static TheoryData<ErrorDominio, TipoError> ErroresDeAplicacion => new()
    {
        { ErroresAplicacion.ClienteNoEncontrado("id"), TipoError.NoEncontrado },
        { ErroresAplicacion.FacturaNoEncontrada("id"), TipoError.NoEncontrado },
        { ErroresAplicacion.EjecucionEnCurso, TipoError.Conflicto },
        { ErroresAplicacion.NumeroFacturaDuplicado("FAC-1"), TipoError.Conflicto },
        { ErroresAplicacion.FiltroInvalido("detalle"), TipoError.Validacion },
        { ErroresAplicacion.SeleccionVacia, TipoError.Validacion },
    };

    [Theory]
    [MemberData(nameof(ErroresDeAplicacion))]
    public void ErrorDeAplicacion_DeclaraElTipoQueLeCorresponde(ErrorDominio error, TipoError esperado) =>
        Assert.Equal(esperado, error.Tipo);
}

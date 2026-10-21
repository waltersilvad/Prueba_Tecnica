using Facturacion.Domain.Clientes;
using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;

namespace Facturacion.Domain.Tests.Comun;

/// <summary>
/// Cada error del dominio declara su tipo: de él depende cómo lo traducen otras capas (por ejemplo, a un código HTTP).
/// </summary>
public sealed class CatalogoErroresTests
{
    public static TheoryData<ErrorDominio, TipoError> ErroresDelDominio => new()
    {
        { ErroresCliente.EmailInvalido, TipoError.Validacion },
        { ErroresCliente.CampoRequerido("Nombre"), TipoError.Validacion },
        { ErroresFactura.MontoNoPositivo, TipoError.Validacion },
        { ErroresFactura.MonedaInvalida, TipoError.Validacion },
        { ErroresFactura.VencimientoAnteriorAEmision, TipoError.Validacion },
        { ErroresFactura.EstadoInvalido, TipoError.Validacion },
        { ErroresFactura.CampoRequerido("Numero"), TipoError.Validacion },
        { ErroresFactura.EstadoDesconocido("pagada"), TipoError.Validacion },
        { ErroresFactura.EstadoFinal, TipoError.Conflicto },
    };

    [Theory]
    [MemberData(nameof(ErroresDelDominio))]
    public void ErrorDelDominio_DeclaraElTipoQueLeCorresponde(ErrorDominio error, TipoError esperado) =>
        Assert.Equal(esperado, error.Tipo);
}

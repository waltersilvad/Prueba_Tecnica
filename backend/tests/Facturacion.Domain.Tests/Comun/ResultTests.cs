using Facturacion.Domain.Comun;

namespace Facturacion.Domain.Tests.Comun;

public sealed class ResultTests
{
    private static readonly ErrorDominio ErrorDePrueba = new("Prueba.Error", "Mensaje de prueba.", TipoError.Validacion);

    [Fact]
    public void Exito_EsExitosoYNoTieneError()
    {
        var resultado = Result.Exito();

        Assert.True(resultado.EsExitoso);
        Assert.False(resultado.EsFallido);
        Assert.Equal(ErrorDominio.Ninguno, resultado.Error);
    }

    [Fact]
    public void Fallo_EsFallidoYConservaElError()
    {
        var resultado = Result.Fallo(ErrorDePrueba);

        Assert.True(resultado.EsFallido);
        Assert.False(resultado.EsExitoso);
        Assert.Equal(ErrorDePrueba, resultado.Error);
    }

    [Fact]
    public void Fallo_ConErrorNinguno_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Fallo(ErrorDominio.Ninguno));
    }

    [Fact]
    public void Fallo_ConErrorNulo_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Fallo(null!));
    }

    [Fact]
    public void ExitoConValor_ExponeElValor()
    {
        var resultado = Result.Exito(42);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(42, resultado.Valor);
        Assert.Equal(ErrorDominio.Ninguno, resultado.Error);
    }

    [Fact]
    public void FalloConValor_ConservaElError()
    {
        var resultado = Result.Fallo<int>(ErrorDePrueba);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErrorDePrueba, resultado.Error);
    }

    [Fact]
    public void FalloConValor_AlLeerElValor_LanzaInvalidOperationException()
    {
        var resultado = Result.Fallo<int>(ErrorDePrueba);

        Assert.Throws<InvalidOperationException>(() => resultado.Valor);
    }

    [Fact]
    public void FalloConValor_ConErrorNinguno_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Fallo<int>(ErrorDominio.Ninguno));
    }
}

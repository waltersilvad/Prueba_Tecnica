using Facturacion.Application.Correo;

namespace Facturacion.Application.Tests.Correo;

public sealed class ValidadorOpcionesPlantillaCorreoTests
{
    private readonly ValidadorOpcionesPlantillaCorreo _validador = new();

    [Fact]
    public void Validate_ConZonaYCulturaValidas_Aprueba()
    {
        var resultado = _validador.Validate(null, Opciones("America/Bogota", "es-CO"));

        Assert.True(resultado.Succeeded);
    }

    [Theory]
    [InlineData("", "es-CO", "ZonaHoraria")]
    [InlineData("Marte/Olimpo", "es-CO", "ZonaHoraria")]
    [InlineData("America/Bogota", "", "Cultura")]
    [InlineData("America/Bogota", "xx-NOEXISTE", "Cultura")]
    public void Validate_ConValorInvalido_FallaIndicandoElCampo(string zona, string cultura, string campo)
    {
        var resultado = _validador.Validate(null, Opciones(zona, cultura));

        Assert.True(resultado.Failed);
        Assert.Contains(campo, resultado.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ConAmbosValoresInvalidos_ReportaLosDosErrores()
    {
        var resultado = _validador.Validate(null, Opciones("", ""));

        Assert.Equal(2, resultado.Failures!.Count());
    }

    private static OpcionesPlantillaCorreo Opciones(string zona, string cultura) =>
        new() { ZonaHoraria = zona, Cultura = cultura };
}

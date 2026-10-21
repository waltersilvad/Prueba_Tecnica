using Facturacion.Domain.Facturas;

namespace Facturacion.Domain.Tests.Facturas;

public sealed class EstadoFacturaTextosTests
{
    [Theory]
    [InlineData(EstadoFactura.PrimerRecordatorio, "primerrecordatorio")]
    [InlineData(EstadoFactura.SegundoRecordatorio, "segundorecordatorio")]
    [InlineData(EstadoFactura.Desactivado, "desactivado")]
    public void ATexto_DevuelveElTextoExactoDelNegocio(EstadoFactura estado, string textoEsperado)
    {
        Assert.Equal(textoEsperado, estado.ATexto());
    }

    [Fact]
    public void ATexto_ConValorNoDefinido_LanzaArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((EstadoFactura)99).ATexto());
    }

    [Theory]
    [InlineData("primerrecordatorio", EstadoFactura.PrimerRecordatorio)]
    [InlineData("segundorecordatorio", EstadoFactura.SegundoRecordatorio)]
    [InlineData("desactivado", EstadoFactura.Desactivado)]
    public void DesdeTexto_ConTextoConocido_DevuelveElEstado(string texto, EstadoFactura estadoEsperado)
    {
        var resultado = EstadoFacturaTextos.DesdeTexto(texto);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(estadoEsperado, resultado.Valor);
    }

    [Theory]
    [InlineData("PrimerRecordatorio")]
    [InlineData(" desactivado")]
    [InlineData("pagada")]
    [InlineData("")]
    [InlineData(null)]
    public void DesdeTexto_ConTextoDesconocido_FallaConEstadoDesconocido(string? texto)
    {
        var resultado = EstadoFacturaTextos.DesdeTexto(texto);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresFactura.EstadoDesconocido(texto), resultado.Error);
    }

    [Theory]
    [InlineData(EstadoFactura.PrimerRecordatorio)]
    [InlineData(EstadoFactura.SegundoRecordatorio)]
    [InlineData(EstadoFactura.Desactivado)]
    public void ATextoYDesdeTexto_SonInversos(EstadoFactura estado)
    {
        Assert.Equal(estado, EstadoFacturaTextos.DesdeTexto(estado.ATexto()).Valor);
    }
}

using Facturacion.Application.Envios;
using Facturacion.Domain.Facturas;
using NSubstitute;

namespace Facturacion.Application.Tests.Envios;

public sealed class ConsultarEnviosUseCaseTests
{
    private readonly IEnvioRepository _envios = Substitute.For<IEnvioRepository>();
    private readonly ConsultarEnviosUseCase _useCase;

    public ConsultarEnviosUseCaseTests() => _useCase = new ConsultarEnviosUseCase(_envios);

    [Fact]
    public async Task EjecutarAsync_DevuelveElHistorialDelClienteConTextosLegibles()
    {
        var registro = new RegistroEnvio(
            "ejecucion-1",
            "cliente-1",
            "factura-1",
            TipoAviso.Desactivacion,
            "pagos@example.com",
            Datos.Ahora,
            ResultadoEnvio.Fallido,
            "SMTP no disponible");
        _envios.ObtenerAsync("cliente-1", Arg.Any<CancellationToken>()).Returns([registro]);

        var resultado = await _useCase.EjecutarAsync("cliente-1", CancellationToken.None);

        Assert.Equal(
            new EnvioDto(
                "ejecucion-1",
                "cliente-1",
                "factura-1",
                "Desactivacion",
                "pagos@example.com",
                Datos.Ahora,
                "Fallido",
                "SMTP no disponible"),
            Assert.Single(resultado));
    }
}

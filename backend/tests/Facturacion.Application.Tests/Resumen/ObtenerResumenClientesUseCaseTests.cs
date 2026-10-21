using Facturacion.Application.Clientes;
using Facturacion.Application.Facturas;
using Facturacion.Application.Resumen;
using Facturacion.Domain.Facturas;
using NSubstitute;

namespace Facturacion.Application.Tests.Resumen;

public sealed class ObtenerResumenClientesUseCaseTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IFacturaRepository _facturas = Substitute.For<IFacturaRepository>();
    private readonly ObtenerResumenClientesUseCase _useCase;

    public ObtenerResumenClientesUseCaseTests() => _useCase = new ObtenerResumenClientesUseCase(_clientes, _facturas);

    [Fact]
    public async Task EjecutarAsync_CuentaPorEstadoYSumaSoloElMontoPendiente()
    {
        var andina = Datos.CrearCliente("cliente-1", "Comercial Andina");
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).Returns([andina]);
        ConConteos(
            new ConteoFacturas("cliente-1", EstadoFactura.PrimerRecordatorio, "COP", 2, 200_000m),
            new ConteoFacturas("cliente-1", EstadoFactura.SegundoRecordatorio, "COP", 1, 50_000m),
            new ConteoFacturas("cliente-1", EstadoFactura.Desactivado, "COP", 1, 70_000m));

        var resumen = Assert.Single(await _useCase.EjecutarAsync(CancellationToken.None));

        Assert.Equal("cliente-1", resumen.ClienteId);
        Assert.Equal("Comercial Andina", resumen.Nombre);
        Assert.Equal(andina.Email, resumen.Email);
        Assert.Equal(2, resumen.PrimerRecordatorio);
        Assert.Equal(1, resumen.SegundoRecordatorio);
        Assert.Equal(1, resumen.Desactivado);
        Assert.Equal(4, resumen.TotalFacturas);
        Assert.Equal([new MontoPorMonedaDto("COP", 250_000m)], resumen.MontoPendiente);
    }

    [Fact]
    public async Task EjecutarAsync_SeparaElMontoPendientePorMoneda()
    {
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).Returns([Datos.CrearCliente("cliente-1")]);
        ConConteos(
            new ConteoFacturas("cliente-1", EstadoFactura.PrimerRecordatorio, "USD", 1, 10m),
            new ConteoFacturas("cliente-1", EstadoFactura.PrimerRecordatorio, "COP", 1, 100_000m),
            new ConteoFacturas("cliente-1", EstadoFactura.SegundoRecordatorio, "USD", 1, 5m));

        var resumen = Assert.Single(await _useCase.EjecutarAsync(CancellationToken.None));

        Assert.Equal(2, resumen.PrimerRecordatorio);
        Assert.Equal(3, resumen.TotalFacturas);
        Assert.Equal(
            [new MontoPorMonedaDto("COP", 100_000m), new MontoPorMonedaDto("USD", 15m)],
            resumen.MontoPendiente);
    }

    [Fact]
    public async Task EjecutarAsync_IncluyeClientesSinFacturasEnElOrdenDelRepositorio()
    {
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).Returns(
        [
            Datos.CrearCliente("cliente-1", "Comercial Andina"),
            Datos.CrearCliente("cliente-2", "Distribuidora Sur"),
        ]);
        ConConteos(new ConteoFacturas("cliente-2", EstadoFactura.Desactivado, "COP", 3, 90_000m));

        var resumen = await _useCase.EjecutarAsync(CancellationToken.None);

        Assert.Equal(["cliente-1", "cliente-2"], resumen.Select(r => r.ClienteId));
        Assert.Equal(0, resumen[0].TotalFacturas);
        Assert.Empty(resumen[0].MontoPendiente);
        Assert.Equal(3, resumen[1].Desactivado);
        Assert.Empty(resumen[1].MontoPendiente);
    }

    private void ConConteos(params ConteoFacturas[] conteos) =>
        _facturas.ContarPorClienteAsync(Arg.Any<CancellationToken>()).Returns(conteos);
}

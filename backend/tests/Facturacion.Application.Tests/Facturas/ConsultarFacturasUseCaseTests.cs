using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Facturas;
using NSubstitute;

namespace Facturacion.Application.Tests.Facturas;

public sealed class ConsultarFacturasUseCaseTests
{
    private readonly IFacturaRepository _facturas = Substitute.For<IFacturaRepository>();
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly ConsultarFacturasUseCase _useCase;

    public ConsultarFacturasUseCaseTests()
    {
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).Returns(
        [
            Datos.CrearCliente("cliente-1", "Comercial Andina"),
            Datos.CrearCliente("cliente-2", "Distribuidora Sur"),
        ]);
        _facturas.BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>()).Returns(LecturaFacturas.Con());
        _useCase = new ConsultarFacturasUseCase(_facturas, _clientes);
    }

    [Fact]
    public async Task EjecutarAsync_SinFiltros_DevuelveLasFacturasConElNombreDelClienteOrdenadasPorNumero()
    {
        var deSur = Datos.CrearFactura("FAC-0002", EstadoFactura.SegundoRecordatorio, "cliente-2", 75_000m);
        var deAndina = Datos.CrearFactura("FAC-0001", EstadoFactura.PrimerRecordatorio, "cliente-1", 150_000m);
        _facturas.BuscarAsync(new FiltroFacturas(), Arg.Any<CancellationToken>()).Returns(LecturaFacturas.Con(deSur, deAndina));

        var resultado = await _useCase.EjecutarAsync(null, null, CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(["FAC-0001", "FAC-0002"], resultado.Valor.Select(f => f.Numero));
        Assert.Equal(
            new FacturaDto(
                "id-FAC-0002",
                "FAC-0002",
                "cliente-2",
                "Distribuidora Sur",
                75_000m,
                "COP",
                deSur.FechaEmision,
                deSur.FechaVencimiento,
                "segundorecordatorio",
                deSur.FechaActualizacion),
            resultado.Valor[1]);
    }

    [Fact]
    public async Task EjecutarAsync_ConEstadoYCliente_AplicaAmbosFiltros()
    {
        await _useCase.EjecutarAsync("desactivado", "cliente-2", CancellationToken.None);

        await _facturas.Received(1).BuscarAsync(
            new FiltroFacturas("cliente-2", EstadoFactura.Desactivado), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task EjecutarAsync_ConFiltrosVacios_NoFiltra(string vacio)
    {
        await _useCase.EjecutarAsync(vacio, vacio, CancellationToken.None);

        await _facturas.Received(1).BuscarAsync(new FiltroFacturas(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConEstadoDesconocido_FallaConFiltroInvalidoSinConsultar()
    {
        var resultado = await _useCase.EjecutarAsync("pagada", null, CancellationToken.None);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresAplicacion.CodigoFiltroInvalido, resultado.Error.Codigo);
        Assert.Contains("pagada", resultado.Error.Mensaje, StringComparison.Ordinal);
        await _facturas.DidNotReceive().BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_FacturaDeClienteInexistente_DevuelveNombreVacio()
    {
        _facturas.BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>())
            .Returns(LecturaFacturas.Con(Datos.CrearFactura("FAC-0009", clienteId: "huerfano")));

        var resultado = await _useCase.EjecutarAsync(null, null, CancellationToken.None);

        Assert.Equal(string.Empty, Assert.Single(resultado.Valor).NombreCliente);
    }
}

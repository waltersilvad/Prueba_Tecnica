using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Correo;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Facturas;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Facturacion.Application.Tests.Facturas;

public sealed class RegistrarFacturaUseCaseTests
{
    private readonly IFacturaRepository _facturas = Substitute.For<IFacturaRepository>();
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IGeneradorId _generadorId = Substitute.For<IGeneradorId>();
    private readonly IReloj _reloj = Substitute.For<IReloj>();
    private readonly RegistrarFacturaUseCase _useCase;

    public RegistrarFacturaUseCaseTests()
    {
        _generadorId.NuevoId().Returns("factura-nueva");
        _reloj.AhoraUtc.Returns(Datos.Ahora);
        _clientes.ObtenerPorIdAsync("cliente-1", Arg.Any<CancellationToken>())
            .Returns(Datos.CrearCliente("cliente-1", "Comercial Andina"));
        _facturas.IntentarAgregarAsync(Arg.Any<Factura>(), Arg.Any<CancellationToken>()).Returns(true);

        var calendario = new CalendarioNegocio(Options.Create(new OpcionesPlantillaCorreo
        {
            ZonaHoraria = "America/Bogota",
            Cultura = "es-CO",
        }));
        _useCase = new RegistrarFacturaUseCase(_facturas, _clientes, _generadorId, _reloj, calendario);
    }

    private static RegistrarFacturaComando Comando(
        string clienteId = "cliente-1", string numero = "FAC-9001", decimal monto = 500_000.50m, string moneda = "COP") =>
        new(clienteId, numero, monto, moneda, new DateOnly(2026, 10, 21), new DateOnly(2026, 11, 20));

    [Fact]
    public async Task EjecutarAsync_ConDatosValidos_GuardaLaFacturaEnPrimerRecordatorio()
    {
        var resultado = await _useCase.EjecutarAsync(Comando(), CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(
            new FacturaDto(
                "factura-nueva",
                "FAC-9001",
                "cliente-1",
                "Comercial Andina",
                500_000.50m,
                "COP",
                new DateTime(2026, 10, 21, 5, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 11, 20, 5, 0, 0, DateTimeKind.Utc),
                "primerrecordatorio",
                Datos.Ahora),
            resultado.Valor);
        await _facturas.Received(1).IntentarAgregarAsync(
            Arg.Is<Factura>(f => f.Id == "factura-nueva" && f.Estado == EstadoFactura.PrimerRecordatorio),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConClienteInexistente_FallaSinGuardar()
    {
        var resultado = await _useCase.EjecutarAsync(Comando(clienteId: "no-existe"), CancellationToken.None);

        Assert.Equal(ErroresAplicacion.ClienteNoEncontrado("no-existe"), resultado.Error);
        await _facturas.DidNotReceive().IntentarAgregarAsync(Arg.Any<Factura>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, "COP")]
    [InlineData(100, "cop")]
    public async Task EjecutarAsync_ConDatosInvalidos_FallaConElErrorDelDominioSinGuardar(decimal monto, string moneda)
    {
        var resultado = await _useCase.EjecutarAsync(Comando(monto: monto, moneda: moneda), CancellationToken.None);

        Assert.True(resultado.EsFallido);
        Assert.StartsWith("Factura.", resultado.Error.Codigo, StringComparison.Ordinal);
        await _facturas.DidNotReceive().IntentarAgregarAsync(Arg.Any<Factura>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConVencimientoAnteriorALaEmision_Falla()
    {
        var comando = Comando() with { FechaVencimiento = new DateOnly(2026, 10, 20) };

        var resultado = await _useCase.EjecutarAsync(comando, CancellationToken.None);

        Assert.Equal(ErroresFactura.VencimientoAnteriorAEmision, resultado.Error);
    }

    [Fact]
    public async Task EjecutarAsync_ConNumeroRepetido_FallaConNumeroDuplicado()
    {
        _facturas.IntentarAgregarAsync(Arg.Any<Factura>(), Arg.Any<CancellationToken>()).Returns(false);

        var resultado = await _useCase.EjecutarAsync(Comando(numero: "FAC-1001"), CancellationToken.None);

        Assert.Equal(ErroresAplicacion.NumeroFacturaDuplicado("FAC-1001"), resultado.Error);
    }

    [Fact]
    public async Task EjecutarAsync_ConComandoNulo_LanzaArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _useCase.EjecutarAsync(null!, CancellationToken.None));
    }
}

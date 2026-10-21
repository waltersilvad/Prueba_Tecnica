using Facturacion.Application.Comun;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Facturas;
using NSubstitute;

namespace Facturacion.Application.Tests.Facturas;

public sealed class EliminarFacturasUseCaseTests
{
    private readonly IFacturaRepository _facturas = Substitute.For<IFacturaRepository>();
    private readonly IEnvioRepository _envios = Substitute.For<IEnvioRepository>();
    private readonly IBloqueoEjecucion _bloqueo = Substitute.For<IBloqueoEjecucion>();
    private readonly IDisposable _liberadorBloqueo = Substitute.For<IDisposable>();
    private readonly EliminarFacturasUseCase _useCase;

    public EliminarFacturasUseCaseTests()
    {
        _bloqueo.IntentarAdquirir().Returns(_liberadorBloqueo);
        _useCase = new EliminarFacturasUseCase(_facturas, _envios, _bloqueo);
    }

    private void ConFacturas(LecturaFacturas lectura) =>
        _facturas.BuscarAsync(Arg.Is<FiltroFacturas>(f => f.FacturaIds != null), Arg.Any<CancellationToken>())
            .Returns(lectura);

    [Fact]
    public async Task EjecutarAsync_EliminaPrimeroLosEnviosYLuegoLasFacturas()
    {
        var f1 = Datos.CrearFactura("F1");
        var f2 = Datos.CrearFactura("F2", EstadoFactura.Desactivado);
        ConFacturas(LecturaFacturas.Con(f1, f2));
        _envios.EliminarPorFacturasAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns(3);
        _facturas.EliminarAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns(2);

        var resultado = await _useCase.EjecutarAsync([f2.Id, f1.Id], CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(["F1", "F2"], resultado.Valor.Numeros);
        Assert.Equal(2, resultado.Valor.FacturasEliminadas);
        Assert.Equal(3, resultado.Valor.EnviosEliminados);
        Received.InOrder(() =>
        {
            _envios.EliminarPorFacturasAsync(
                Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2), Arg.Any<CancellationToken>());
            _facturas.EliminarAsync(
                Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task EjecutarAsync_TambienEliminaFacturasQueNoSePuedenInterpretar()
    {
        ConFacturas(new LecturaFacturas([], [new FacturaNoInterpretable("id-F9", "F9", ErroresFactura.EstadoDesconocido("pagada"))]));

        var resultado = await _useCase.EjecutarAsync(["id-F9"], CancellationToken.None);

        Assert.Equal(["F9"], resultado.Valor.Numeros);
        await _facturas.Received(1).EliminarAsync(
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("id-F9")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConIdsRepetidos_LosEliminaUnaSolaVez()
    {
        var f1 = Datos.CrearFactura("F1");
        ConFacturas(LecturaFacturas.Con(f1));

        await _useCase.EjecutarAsync([f1.Id, f1.Id], CancellationToken.None);

        await _facturas.Received(1).EliminarAsync(
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConUnaFacturaInexistente_NoEliminaNinguna()
    {
        var f1 = Datos.CrearFactura("F1");
        ConFacturas(LecturaFacturas.Con(f1));

        var resultado = await _useCase.EjecutarAsync([f1.Id, "no-existe"], CancellationToken.None);

        Assert.Equal(ErroresAplicacion.FacturaNoEncontrada("no-existe"), resultado.Error);
        await _facturas.DidNotReceive().EliminarAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        await _envios.DidNotReceive().EliminarPorFacturasAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConSeleccionVacia_FallaSinTomarElBloqueo()
    {
        var resultado = await _useCase.EjecutarAsync([], CancellationToken.None);

        Assert.Equal(ErroresAplicacion.SeleccionVacia, resultado.Error);
        _bloqueo.DidNotReceive().IntentarAdquirir();
    }

    [Fact]
    public async Task EjecutarAsync_ConUnProcesoEnCurso_FallaSinEliminarNada()
    {
        _bloqueo.IntentarAdquirir().Returns((IDisposable?)null);

        var resultado = await _useCase.EjecutarAsync(["id-F1"], CancellationToken.None);

        Assert.Equal(ErroresAplicacion.EjecucionEnCurso, resultado.Error);
        await _facturas.DidNotReceive().BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_AlTerminar_LiberaElBloqueo()
    {
        ConFacturas(LecturaFacturas.Con(Datos.CrearFactura("F1")));

        await _useCase.EjecutarAsync(["id-F1"], CancellationToken.None);

        _liberadorBloqueo.Received(1).Dispose();
    }
}

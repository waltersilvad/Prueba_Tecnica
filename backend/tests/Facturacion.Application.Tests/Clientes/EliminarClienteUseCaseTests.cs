using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Clientes;
using NSubstitute;

namespace Facturacion.Application.Tests.Clientes;

public sealed class EliminarClienteUseCaseTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IFacturaRepository _facturas = Substitute.For<IFacturaRepository>();
    private readonly IEnvioRepository _envios = Substitute.For<IEnvioRepository>();
    private readonly IBloqueoEjecucion _bloqueo = Substitute.For<IBloqueoEjecucion>();
    private readonly IDisposable _liberadorBloqueo = Substitute.For<IDisposable>();
    private readonly EliminarClienteUseCase _useCase;

    public EliminarClienteUseCaseTests()
    {
        _bloqueo.IntentarAdquirir().Returns(_liberadorBloqueo);
        _useCase = new EliminarClienteUseCase(_clientes, _facturas, _envios, _bloqueo);
    }

    [Fact]
    public async Task EjecutarAsync_EliminaPrimeroEnviosYFacturasYLuegoElCliente()
    {
        var cliente = Datos.CrearCliente("cliente-1", "Comercial Andina");
        _clientes.ObtenerPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
        _envios.EliminarPorClienteAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(5);
        _facturas.EliminarPorClienteAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(4);
        _clientes.EliminarAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await _useCase.EjecutarAsync(cliente.Id, CancellationToken.None);

        Assert.Equal(new ResultadoEliminacionCliente("cliente-1", "Comercial Andina", 4, 5), resultado.Valor);
        // Primero lo que depende del cliente: si algo falla a mitad, el cliente sigue existiendo y se puede reintentar.
        Received.InOrder(() =>
        {
            _envios.EliminarPorClienteAsync(cliente.Id, Arg.Any<CancellationToken>());
            _facturas.EliminarPorClienteAsync(cliente.Id, Arg.Any<CancellationToken>());
            _clientes.EliminarAsync(cliente.Id, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task EjecutarAsync_ConClienteInexistente_FallaSinEliminarNada()
    {
        _clientes.ObtenerPorIdAsync("no-existe", Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var resultado = await _useCase.EjecutarAsync("no-existe", CancellationToken.None);

        Assert.Equal(ErroresAplicacion.ClienteNoEncontrado("no-existe"), resultado.Error);
        await _facturas.DidNotReceive().EliminarPorClienteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _clientes.DidNotReceive().EliminarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConUnProcesoDeAvisosEnCurso_FallaSinEliminarNada()
    {
        _bloqueo.IntentarAdquirir().Returns((IDisposable?)null);

        var resultado = await _useCase.EjecutarAsync("cliente-1", CancellationToken.None);

        Assert.Equal(ErroresAplicacion.EjecucionEnCurso, resultado.Error);
        await _clientes.DidNotReceive().ObtenerPorIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_AlTerminar_LiberaElBloqueo()
    {
        _clientes.ObtenerPorIdAsync("cliente-1", Arg.Any<CancellationToken>()).Returns(Datos.CrearCliente());

        await _useCase.EjecutarAsync("cliente-1", CancellationToken.None);

        _liberadorBloqueo.Received(1).Dispose();
    }
}

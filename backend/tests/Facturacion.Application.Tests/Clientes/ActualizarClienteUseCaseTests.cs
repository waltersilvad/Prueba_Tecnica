using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Domain.Clientes;
using NSubstitute;

namespace Facturacion.Application.Tests.Clientes;

public sealed class ActualizarClienteUseCaseTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IReloj _reloj = Substitute.For<IReloj>();
    private readonly ActualizarClienteUseCase _useCase;

    public ActualizarClienteUseCaseTests()
    {
        _reloj.AhoraUtc.Returns(Datos.Ahora);
        _useCase = new ActualizarClienteUseCase(_clientes, _reloj);
    }

    [Fact]
    public async Task EjecutarAsync_ConDatosValidos_GuardaYDevuelveElClienteActualizado()
    {
        var cliente = Datos.CrearCliente();
        _clientes.ObtenerPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var resultado = await _useCase.EjecutarAsync(
            cliente.Id, new ActualizarClienteComando("Distribuidora Sur", "cartera@example.com"), CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(
            new ClienteDto(cliente.Id, "Distribuidora Sur", "cartera@example.com", Datos.Ahora),
            resultado.Valor);
        await _clientes.Received(1).ActualizarAsync(
            Arg.Is<Cliente>(c => c.Id == cliente.Id && c.Email == "cartera@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConClienteInexistente_FallaSinGuardar()
    {
        _clientes.ObtenerPorIdAsync("no-existe", Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var resultado = await _useCase.EjecutarAsync(
            "no-existe", new ActualizarClienteComando("Nombre", "correo@example.com"), CancellationToken.None);

        Assert.Equal(ErroresAplicacion.ClienteNoEncontrado("no-existe"), resultado.Error);
        await _clientes.DidNotReceive().ActualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConEmailInvalido_FallaSinGuardar()
    {
        var cliente = Datos.CrearCliente();
        _clientes.ObtenerPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var resultado = await _useCase.EjecutarAsync(
            cliente.Id, new ActualizarClienteComando("Distribuidora Sur", "correo-invalido"), CancellationToken.None);

        Assert.Equal(ErroresCliente.EmailInvalido, resultado.Error);
        await _clientes.DidNotReceive().ActualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConComandoNulo_LanzaArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _useCase.EjecutarAsync("cliente-1", null!, CancellationToken.None));
    }
}

using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Domain.Clientes;
using NSubstitute;

namespace Facturacion.Application.Tests.Clientes;

public sealed class CrearClienteUseCaseTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IGeneradorId _generadorId = Substitute.For<IGeneradorId>();
    private readonly IReloj _reloj = Substitute.For<IReloj>();
    private readonly CrearClienteUseCase _useCase;

    public CrearClienteUseCaseTests()
    {
        _generadorId.NuevoId().Returns("cliente-nuevo");
        _reloj.AhoraUtc.Returns(Datos.Ahora);
        _useCase = new CrearClienteUseCase(_clientes, _generadorId, _reloj);
    }

    [Fact]
    public async Task EjecutarAsync_ConDatosValidos_GuardaYDevuelveElClienteNuevo()
    {
        var resultado = await _useCase.EjecutarAsync(
            new CrearClienteComando(" Industrias Pacífico ", "pagos@pacifico.example.com"), CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(
            new ClienteDto("cliente-nuevo", "Industrias Pacífico", "pagos@pacifico.example.com", Datos.Ahora),
            resultado.Valor);
        await _clientes.Received(1).AgregarAsync(
            Arg.Is<Cliente>(c => c.Id == "cliente-nuevo" && c.Nombre == "Industrias Pacífico"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConCorreoInvalido_FallaSinGuardar()
    {
        var resultado = await _useCase.EjecutarAsync(
            new CrearClienteComando("Industrias Pacífico", "correo-invalido"), CancellationToken.None);

        Assert.Equal(ErroresCliente.EmailInvalido, resultado.Error);
        await _clientes.DidNotReceive().AgregarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConComandoNulo_LanzaArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _useCase.EjecutarAsync(null!, CancellationToken.None));
    }
}

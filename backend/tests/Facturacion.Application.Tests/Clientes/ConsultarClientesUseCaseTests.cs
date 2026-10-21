using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Domain.Clientes;
using NSubstitute;

namespace Facturacion.Application.Tests.Clientes;

public sealed class ConsultarClientesUseCaseTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly ConsultarClientesUseCase _useCase;

    public ConsultarClientesUseCaseTests() => _useCase = new ConsultarClientesUseCase(_clientes);

    [Fact]
    public async Task ListarAsync_DevuelveTodosLosClientesComoDto()
    {
        var andina = Datos.CrearCliente("cliente-1", "Comercial Andina");
        var sur = Datos.CrearCliente("cliente-2", "Distribuidora Sur");
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).Returns([andina, sur]);

        var resultado = await _useCase.ListarAsync(CancellationToken.None);

        Assert.Equal([ClienteDto.Desde(andina), ClienteDto.Desde(sur)], resultado);
    }

    [Fact]
    public async Task ObtenerAsync_ConClienteExistente_DevuelveElDto()
    {
        var cliente = Datos.CrearCliente();
        _clientes.ObtenerPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

        var resultado = await _useCase.ObtenerAsync(cliente.Id, CancellationToken.None);

        Assert.Equal(new ClienteDto(cliente.Id, cliente.Nombre, cliente.Email, cliente.FechaActualizacion), resultado.Valor);
    }

    [Fact]
    public async Task ObtenerAsync_ConClienteInexistente_FallaConClienteNoEncontrado()
    {
        _clientes.ObtenerPorIdAsync("no-existe", Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var resultado = await _useCase.ObtenerAsync("no-existe", CancellationToken.None);

        Assert.Equal(ErroresAplicacion.ClienteNoEncontrado("no-existe"), resultado.Error);
    }
}

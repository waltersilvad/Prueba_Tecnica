using Facturacion.Application.Comun;
using Facturacion.Domain.Comun;

namespace Facturacion.Application.Clientes;

/// <summary>
/// Consultas de clientes.
/// </summary>
public sealed class ConsultarClientesUseCase(IClienteRepository clientes)
{
    /// <summary>Lista todos los clientes.</summary>
    public async Task<IReadOnlyList<ClienteDto>> ListarAsync(CancellationToken cancellationToken)
    {
        var todos = await clientes.ObtenerTodosAsync(cancellationToken);
        return [.. todos.Select(ClienteDto.Desde)];
    }

    /// <summary>Obtiene un cliente, o falla con <see cref="ErroresAplicacion.CodigoClienteNoEncontrado"/>.</summary>
    public async Task<Result<ClienteDto>> ObtenerAsync(string id, CancellationToken cancellationToken)
    {
        var cliente = await clientes.ObtenerPorIdAsync(id, cancellationToken);
        return cliente is null
            ? Result.Fallo<ClienteDto>(ErroresAplicacion.ClienteNoEncontrado(id))
            : Result.Exito(ClienteDto.Desde(cliente));
    }
}

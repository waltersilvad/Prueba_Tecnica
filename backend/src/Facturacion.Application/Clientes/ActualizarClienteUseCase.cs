using Facturacion.Application.Comun;
using Facturacion.Domain.Comun;

namespace Facturacion.Application.Clientes;

/// <summary>Nuevos datos de contacto de un cliente.</summary>
public sealed record ActualizarClienteComando(string Nombre, string Email);

/// <summary>
/// Edita nombre y correo de un cliente. Las reglas de validación viven en la entidad <c>Cliente</c>.
/// </summary>
public sealed class ActualizarClienteUseCase(IClienteRepository clientes, IReloj reloj)
{
    /// <summary>
    /// Aplica los cambios y los guarda. Falla si el cliente no existe o los datos no son válidos.
    /// </summary>
    public async Task<Result<ClienteDto>> EjecutarAsync(
        string id, ActualizarClienteComando comando, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var cliente = await clientes.ObtenerPorIdAsync(id, cancellationToken);
        if (cliente is null)
        {
            return Result.Fallo<ClienteDto>(ErroresAplicacion.ClienteNoEncontrado(id));
        }

        var actualizacion = cliente.ActualizarDatos(comando.Nombre, comando.Email, reloj.AhoraUtc);
        if (actualizacion.EsFallido)
        {
            return Result.Fallo<ClienteDto>(actualizacion.Error);
        }

        await clientes.ActualizarAsync(cliente, cancellationToken);
        return Result.Exito(ClienteDto.Desde(cliente));
    }
}

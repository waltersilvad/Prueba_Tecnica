using Facturacion.Application.Comun;
using Facturacion.Domain.Clientes;
using Facturacion.Domain.Comun;

namespace Facturacion.Application.Clientes;

/// <summary>Datos de un cliente nuevo.</summary>
public sealed record CrearClienteComando(string Nombre, string Email);

/// <summary>
/// Registra un cliente nuevo. Las reglas de validación viven en la entidad <see cref="Cliente"/>.
/// </summary>
public sealed class CrearClienteUseCase(IClienteRepository clientes, IGeneradorId generadorId, IReloj reloj)
{
    /// <summary>Crea y guarda el cliente, o falla si los datos no son válidos.</summary>
    public async Task<Result<ClienteDto>> EjecutarAsync(CrearClienteComando comando, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var cliente = Cliente.Crear(generadorId.NuevoId(), comando.Nombre, comando.Email, reloj.AhoraUtc);
        if (cliente.EsFallido)
        {
            return Result.Fallo<ClienteDto>(cliente.Error);
        }

        await clientes.AgregarAsync(cliente.Valor, cancellationToken);
        return Result.Exito(ClienteDto.Desde(cliente.Valor));
    }
}

using Facturacion.Domain.Clientes;

namespace Facturacion.Application.Clientes;

/// <summary>
/// Persistencia de clientes.
/// </summary>
public interface IClienteRepository
{
    /// <summary>Obtiene todos los clientes ordenados por nombre.</summary>
    Task<IReadOnlyList<Cliente>> ObtenerTodosAsync(CancellationToken cancellationToken);

    /// <summary>Obtiene un cliente por su id, o <see langword="null"/> si no existe o el id no es válido.</summary>
    Task<Cliente?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>Guarda un cliente nuevo.</summary>
    Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken);

    /// <summary>Elimina el cliente.</summary>
    /// <returns><see langword="true"/> si existía y se eliminó.</returns>
    Task<bool> EliminarAsync(string id, CancellationToken cancellationToken);

    /// <summary>Guarda nombre, correo y fecha de actualización del cliente.</summary>
    Task ActualizarAsync(Cliente cliente, CancellationToken cancellationToken);
}

using Facturacion.Domain.Clientes;

namespace Facturacion.Application.Clientes;

/// <summary>
/// Datos de un cliente expuestos por la API.
/// </summary>
public sealed record ClienteDto(string Id, string Nombre, string Email, DateTime FechaActualizacion)
{
    public static ClienteDto Desde(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        return new ClienteDto(cliente.Id, cliente.Nombre, cliente.Email, cliente.FechaActualizacion);
    }
}

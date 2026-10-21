using Facturacion.Application.Clientes;
using Facturacion.Domain.Clientes;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Facturacion.Infrastructure.Persistencia;

internal sealed class MongoClienteRepository(ContextoMongo contexto) : IClienteRepository
{
    public async Task<IReadOnlyList<Cliente>> ObtenerTodosAsync(CancellationToken cancellationToken)
    {
        var documentos = await contexto.Clientes
            .Find(FilterDefinition<ClienteDocumento>.Empty)
            .SortBy(d => d.Nombre)
            .ToListAsync(cancellationToken);

        return [.. documentos.Select(MapeoDocumentos.ACliente)];
    }

    public async Task<Cliente?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return null;
        }

        var documento = await contexto.Clientes
            .Find(d => d.Id == objectId)
            .FirstOrDefaultAsync(cancellationToken);

        return documento is null ? null : MapeoDocumentos.ACliente(documento);
    }

    public Task AgregarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        return contexto.Clientes.InsertOneAsync(MapeoDocumentos.ADocumento(cliente), cancellationToken: cancellationToken);
    }

    public async Task<bool> EliminarAsync(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return false;
        }

        var resultado = await contexto.Clientes.DeleteOneAsync(d => d.Id == objectId, cancellationToken);
        return resultado.DeletedCount == 1;
    }

    public Task ActualizarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        var id = ObjectId.Parse(cliente.Id);
        var actualizacion = Builders<ClienteDocumento>.Update
            .Set(d => d.Nombre, cliente.Nombre)
            .Set(d => d.Email, cliente.Email)
            .Set(d => d.FechaActualizacion, cliente.FechaActualizacion);

        return contexto.Clientes.UpdateOneAsync(d => d.Id == id, actualizacion, cancellationToken: cancellationToken);
    }
}

using Facturacion.Application.Envios;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Facturacion.Infrastructure.Persistencia;

internal sealed class MongoEnvioRepository(ContextoMongo contexto) : IEnvioRepository
{
    public Task RegistrarAsync(RegistroEnvio registro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registro);
        return contexto.Envios.InsertOneAsync(
            MapeoDocumentos.ADocumento(registro), cancellationToken: cancellationToken);
    }

    public async Task<long> EliminarPorFacturasAsync(
        IReadOnlyCollection<string> facturaIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(facturaIds);
        var ids = facturaIds
            .Select(id => ObjectId.TryParse(id, out var objectId) ? objectId : (ObjectId?)null)
            .OfType<ObjectId>()
            .ToList();
        if (ids.Count == 0)
        {
            return 0;
        }

        var resultado = await contexto.Envios.DeleteManyAsync(
            Builders<EnvioDocumento>.Filter.In(d => d.FacturaId, ids), cancellationToken);
        return resultado.DeletedCount;
    }

    public async Task<long> EliminarPorClienteAsync(string clienteId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(clienteId, out var id))
        {
            return 0;
        }

        var resultado = await contexto.Envios.DeleteManyAsync(d => d.ClienteId == id, cancellationToken);
        return resultado.DeletedCount;
    }

    public async Task<IReadOnlyList<RegistroEnvio>> ObtenerAsync(string? clienteId, CancellationToken cancellationToken)
    {
        var filtro = FilterDefinition<EnvioDocumento>.Empty;
        if (clienteId is not null)
        {
            if (!ObjectId.TryParse(clienteId, out var id))
            {
                return [];
            }

            filtro = Builders<EnvioDocumento>.Filter.Eq(d => d.ClienteId, id);
        }

        var documentos = await contexto.Envios
            .Find(filtro)
            .SortByDescending(d => d.Fecha)
            .ToListAsync(cancellationToken);

        return [.. documentos.Select(MapeoDocumentos.ARegistro)];
    }
}

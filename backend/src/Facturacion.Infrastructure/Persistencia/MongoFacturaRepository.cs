using Facturacion.Application.Facturas;
using Facturacion.Domain.Facturas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Facturacion.Infrastructure.Persistencia;

internal sealed class MongoFacturaRepository(ContextoMongo contexto) : IFacturaRepository
{
    private static readonly FilterDefinitionBuilder<FacturaDocumento> Filtro = Builders<FacturaDocumento>.Filter;

    public async Task<LecturaFacturas> BuscarAsync(FiltroFacturas filtro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var condicion = Filtro.Empty;

        if (filtro.ClienteId is not null)
        {
            if (!ObjectId.TryParse(filtro.ClienteId, out var clienteId))
            {
                return LecturaFacturas.Con();
            }

            condicion &= Filtro.Eq(d => d.ClienteId, clienteId);
        }

        if (filtro.Estado is { } estado)
        {
            condicion &= Filtro.Eq(d => d.Estado, estado.ATexto());
        }

        if (filtro.FacturaIds is not null)
        {
            // Los ids con formato inválido no pueden existir: se descartan; si no queda ninguno, no hay resultados.
            var ids = AObjectIds(filtro.FacturaIds);
            if (ids.Count == 0)
            {
                return LecturaFacturas.Con();
            }

            condicion &= Filtro.In(d => d.Id, ids);
        }

        var documentos = await contexto.Facturas
            .Find(condicion)
            .SortBy(d => d.Numero)
            .ToListAsync(cancellationToken);

        return MapeoDocumentos.ALectura(documentos);
    }

    public async Task<bool> IntentarAgregarAsync(Factura factura, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factura);

        try
        {
            await contexto.Facturas.InsertOneAsync(MapeoDocumentos.ADocumento(factura), cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // El índice único de "numero" rechaza la factura: es un resultado esperado, no un fallo.
            return false;
        }
    }

    public async Task<bool> ActualizarEstadoAsync(
        Factura factura, EstadoFactura estadoEsperado, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factura);

        // Actualización condicional: solo cambia si nadie la modificó desde que se leyó.
        var condicion = Filtro.Eq(d => d.Id, ObjectId.Parse(factura.Id))
            & Filtro.Eq(d => d.Estado, estadoEsperado.ATexto());

        var actualizacion = Builders<FacturaDocumento>.Update
            .Set(d => d.Estado, factura.Estado.ATexto())
            .Set(d => d.FechaActualizacion, factura.FechaActualizacion);

        var resultado = await contexto.Facturas.UpdateOneAsync(
            condicion, actualizacion, cancellationToken: cancellationToken);

        return resultado.ModifiedCount == 1;
    }

    public async Task<long> EliminarAsync(IReadOnlyCollection<string> facturaIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(facturaIds);
        var ids = AObjectIds(facturaIds);
        if (ids.Count == 0)
        {
            return 0;
        }

        var resultado = await contexto.Facturas.DeleteManyAsync(Filtro.In(d => d.Id, ids), cancellationToken);
        return resultado.DeletedCount;
    }

    public async Task<long> EliminarPorClienteAsync(string clienteId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(clienteId, out var id))
        {
            return 0;
        }

        var resultado = await contexto.Facturas.DeleteManyAsync(d => d.ClienteId == id, cancellationToken);
        return resultado.DeletedCount;
    }

    public async Task<IReadOnlyList<ConteoFacturas>> ContarPorClienteAsync(CancellationToken cancellationToken)
    {
        var grupos = await contexto.Facturas
            .Aggregate()
            .Group(
                d => new { d.ClienteId, d.Estado, d.Moneda },
                g => new
                {
                    g.Key.ClienteId,
                    g.Key.Estado,
                    g.Key.Moneda,
                    Cantidad = g.Count(),
                    MontoTotal = g.Sum(d => d.Monto),
                })
            .ToListAsync(cancellationToken);

        // El esquema de la colección impide estados desconocidos; si aparecieran, no se cuentan.
        return
        [
            .. grupos
                .Select(g => (Grupo: g, Estado: EstadoFacturaTextos.DesdeTexto(g.Estado)))
                .Where(x => x.Estado.EsExitoso)
                .Select(x => new ConteoFacturas(
                    x.Grupo.ClienteId.ToString(),
                    x.Estado.Valor,
                    x.Grupo.Moneda,
                    x.Grupo.Cantidad,
                    x.Grupo.MontoTotal)),
        ];
    }

    /// <summary>Convierte ids de texto en ObjectId, descartando los que no tienen formato válido (no pueden existir).</summary>
    private static List<ObjectId> AObjectIds(IEnumerable<string> ids) =>
        [.. ids.Select(id => ObjectId.TryParse(id, out var objectId) ? objectId : (ObjectId?)null).OfType<ObjectId>()];
}

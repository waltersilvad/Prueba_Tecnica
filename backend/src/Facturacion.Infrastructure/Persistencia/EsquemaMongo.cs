using Facturacion.Domain.Facturas;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Crea o actualiza, de forma idempotente, las colecciones con su <c>$jsonSchema</c> y los índices.
/// </summary>
/// <remarks>
/// El esquema es una segunda línea de defensa: el dominio ya valida, pero así la base rechaza
/// también los datos inválidos que lleguen por fuera de la aplicación.
/// </remarks>
internal sealed class EsquemaMongo(ContextoMongo contexto)
{
    public const string IndiceClienteEstado = "ix_clienteId_estado";
    public const string IndiceNumeroUnico = "ux_numero";
    public const string IndiceEnviosClienteFecha = "ix_clienteId_fecha";

    public async Task AsegurarAsync(CancellationToken cancellationToken)
    {
        await AsegurarColeccionAsync(ContextoMongo.ColeccionClientes, EsquemaClientes(), cancellationToken);
        await AsegurarColeccionAsync(ContextoMongo.ColeccionFacturas, EsquemaFacturas(), cancellationToken);
        await AsegurarColeccionAsync(ContextoMongo.ColeccionEnvios, esquema: null, cancellationToken);
        await AsegurarIndicesAsync(cancellationToken);
    }

    private async Task AsegurarColeccionAsync(string nombre, BsonDocument? esquema, CancellationToken cancellationToken)
    {
        var existentes = await contexto.BaseDatos.ListCollectionNamesAsync(
            new ListCollectionNamesOptions { Filter = new BsonDocument("name", nombre) },
            cancellationToken);

        if (!await existentes.AnyAsync(cancellationToken))
        {
            await contexto.BaseDatos.CreateCollectionAsync(nombre, cancellationToken: cancellationToken);
        }

        if (esquema is null)
        {
            return;
        }

        // collMod reemplaza el validador: aplica el esquema vigente aunque la colección ya existiera.
        var comando = new BsonDocument
        {
            { "collMod", nombre },
            { "validator", new BsonDocument("$jsonSchema", esquema) },
            { "validationLevel", "strict" },
            { "validationAction", "error" },
        };
        await contexto.BaseDatos.RunCommandAsync<BsonDocument>(comando, cancellationToken: cancellationToken);
    }

    private async Task AsegurarIndicesAsync(CancellationToken cancellationToken)
    {
        var facturas = Builders<FacturaDocumento>.IndexKeys;
        await contexto.Facturas.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<FacturaDocumento>(
                    facturas.Ascending(d => d.ClienteId).Ascending(d => d.Estado),
                    new CreateIndexOptions { Name = IndiceClienteEstado }),
                new CreateIndexModel<FacturaDocumento>(
                    facturas.Ascending(d => d.Numero),
                    new CreateIndexOptions { Name = IndiceNumeroUnico, Unique = true }),
            ],
            cancellationToken);

        await contexto.Envios.Indexes.CreateOneAsync(
            new CreateIndexModel<EnvioDocumento>(
                Builders<EnvioDocumento>.IndexKeys.Ascending(d => d.ClienteId).Descending(d => d.Fecha),
                new CreateIndexOptions { Name = IndiceEnviosClienteFecha }),
            cancellationToken: cancellationToken);
    }

    private static BsonDocument EsquemaClientes() => new()
    {
        { "bsonType", "object" },
        { "required", new BsonArray { "nombre", "email", "fechaActualizacion" } },
        {
            "properties", new BsonDocument
            {
                { "nombre", new BsonDocument { { "bsonType", "string" }, { "minLength", 1 } } },
                { "email", new BsonDocument { { "bsonType", "string" }, { "pattern", @"^[^@\s]+@[^@\s]+$" } } },
                { "fechaActualizacion", new BsonDocument("bsonType", "date") },
            }
        },
    };

    private static BsonDocument EsquemaFacturas() => new()
    {
        { "bsonType", "object" },
        {
            "required", new BsonArray
            {
                "numero", "clienteId", "monto", "moneda", "fechaEmision", "fechaVencimiento", "estado", "fechaActualizacion",
            }
        },
        {
            "properties", new BsonDocument
            {
                { "numero", new BsonDocument { { "bsonType", "string" }, { "minLength", 1 } } },
                { "clienteId", new BsonDocument("bsonType", "objectId") },
                {
                    "monto", new BsonDocument
                    {
                        { "bsonType", "decimal" },
                        { "minimum", new BsonDecimal128(0m) },
                        { "exclusiveMinimum", true },
                    }
                },
                { "moneda", new BsonDocument { { "bsonType", "string" }, { "pattern", "^[A-Z]{3}$" } } },
                { "fechaEmision", new BsonDocument("bsonType", "date") },
                { "fechaVencimiento", new BsonDocument("bsonType", "date") },
                {
                    "estado", new BsonDocument
                    {
                        { "bsonType", "string" },
                        {
                            "enum", new BsonArray
                            {
                                EstadoFacturaTextos.PrimerRecordatorio,
                                EstadoFacturaTextos.SegundoRecordatorio,
                                EstadoFacturaTextos.Desactivado,
                            }
                        },
                    }
                },
                { "fechaActualizacion", new BsonDocument("bsonType", "date") },
            }
        },
    };
}

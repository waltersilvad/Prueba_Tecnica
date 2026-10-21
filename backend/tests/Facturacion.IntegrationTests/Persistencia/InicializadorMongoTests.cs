using Facturacion.IntegrationTests.Infraestructura;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Facturacion.IntegrationTests.Persistencia;

[Collection(ColeccionMongo.Nombre)]
public sealed class InicializadorMongoTests(MongoFixture mongo)
{
    private const int CodigoValidacionDocumento = 121;

    [Fact]
    public async Task Arranque_CreaLosIndicesDeFacturasYEnvios()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var indicesFacturas = await ObtenerIndicesAsync(entorno.Base, "facturas");
        var indicesEnvios = await ObtenerIndicesAsync(entorno.Base, "envios");

        var compuesto = indicesFacturas["ix_clienteId_estado"];
        Assert.Equal(new BsonDocument { { "clienteId", 1 }, { "estado", 1 } }, compuesto["key"].AsBsonDocument);

        var unico = indicesFacturas["ux_numero"];
        Assert.Equal(new BsonDocument("numero", 1), unico["key"].AsBsonDocument);
        Assert.True(unico["unique"].AsBoolean);

        Assert.Equal(
            new BsonDocument { { "clienteId", 1 }, { "fecha", -1 } },
            indicesEnvios["ix_clienteId_fecha"]["key"].AsBsonDocument);
    }

    public static TheoryData<string, BsonValue> CamposInvalidos => new()
    {
        { "estado", "pagada" },
        { "monto", new BsonDecimal128(0m) },
        { "monto", new BsonDecimal128(-10m) },
        { "moneda", "pesos" },
    };

    [Theory]
    [MemberData(nameof(CamposInvalidos))]
    public async Task Arranque_AplicaUnEsquemaQueRechazaFacturasInvalidas(string campo, BsonValue valorInvalido)
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var factura = FacturaValida();
        factura[campo] = valorInvalido;

        var error = await Assert.ThrowsAsync<MongoWriteException>(
            () => entorno.Base.GetCollection<BsonDocument>("facturas").InsertOneAsync(factura));

        Assert.Equal(CodigoValidacionDocumento, error.WriteError.Code);
    }

    [Fact]
    public async Task Arranque_AplicaUnEsquemaQueAceptaFacturasValidas()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        await entorno.Base.GetCollection<BsonDocument>("facturas").InsertOneAsync(FacturaValida());
    }

    [Fact]
    public async Task Arranque_AplicaUnEsquemaQueRechazaClientesConCorreoInvalido()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var cliente = new BsonDocument
        {
            { "nombre", "Cliente sin correo" },
            { "email", "no-es-correo" },
            { "fechaActualizacion", DateTime.UtcNow },
        };

        var error = await Assert.ThrowsAsync<MongoWriteException>(
            () => entorno.Base.GetCollection<BsonDocument>("clientes").InsertOneAsync(cliente));

        Assert.Equal(CodigoValidacionDocumento, error.WriteError.Code);
    }

    [Fact]
    public async Task Arranque_EnDevelopment_CargaLaSemillaUnaSolaVez()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        await entorno.ArrancarAsync();

        Assert.Equal(3, await ContarAsync(entorno.Base, "clientes"));
        Assert.Equal(10, await ContarAsync(entorno.Base, "facturas"));
    }

    [Fact]
    public async Task Arranque_FueraDeDevelopment_AplicaElEsquemaPeroNoCargaDatos()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo, "Production");

        Assert.Equal(0, await ContarAsync(entorno.Base, "clientes"));
        Assert.Equal(0, await ContarAsync(entorno.Base, "facturas"));
        Assert.Contains("ux_numero", (await ObtenerIndicesAsync(entorno.Base, "facturas")).Keys);
    }

    private static BsonDocument FacturaValida() => new()
    {
        { "numero", $"FAC-{Guid.NewGuid():N}" },
        { "clienteId", ObjectId.Parse(EntornoPruebas.IdCliente1) },
        { "monto", new BsonDecimal128(1000m) },
        { "moneda", "COP" },
        { "fechaEmision", new DateTime(2026, 9, 1, 5, 0, 0, DateTimeKind.Utc) },
        { "fechaVencimiento", new DateTime(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc) },
        { "estado", "primerrecordatorio" },
        { "fechaActualizacion", new DateTime(2026, 9, 1, 5, 0, 0, DateTimeKind.Utc) },
    };

    private static async Task<Dictionary<string, BsonDocument>> ObtenerIndicesAsync(IMongoDatabase baseDatos, string coleccion)
    {
        var indices = await (await baseDatos.GetCollection<BsonDocument>(coleccion).Indexes.ListAsync()).ToListAsync();
        return indices.ToDictionary(i => i["name"].AsString);
    }

    private static Task<long> ContarAsync(IMongoDatabase baseDatos, string coleccion) =>
        baseDatos.GetCollection<BsonDocument>(coleccion).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
}

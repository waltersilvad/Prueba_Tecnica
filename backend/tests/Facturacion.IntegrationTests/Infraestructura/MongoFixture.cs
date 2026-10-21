using Testcontainers.MongoDb;

namespace Facturacion.IntegrationTests.Infraestructura;

/// <summary>
/// Un contenedor de MongoDB real, compartido por todas las pruebas de la colección.
/// Cada prueba usa su propia base de datos para no interferir con las demás.
/// </summary>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _contenedor = new MongoDbBuilder("mongo:8.0").Build();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public Task InitializeAsync() => _contenedor.StartAsync();

    public Task DisposeAsync() => _contenedor.DisposeAsync().AsTask();
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionMongo : ICollectionFixture<MongoFixture>
{
    public const string Nombre = "Mongo";
}

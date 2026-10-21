using Testcontainers.MongoDb;

namespace Facturacion.IntegrationTests.Infraestructura;

/// <summary>
/// Un contenedor de MongoDB real, compartido por todas las pruebas de la colección.
/// Cada prueba usa su propia base de datos para no interferir con las demás.
/// </summary>
/// <remarks>
/// Sin autenticación, igual que el Mongo del <c>docker-compose.yml</c>. Con usuario y contraseña, la imagen arranca un
/// <c>mongod</c> temporal para crearlos y luego el definitivo; con la máquina cargada (por ejemplo, en CI) el temporal
/// puede no haber liberado el puerto y el contenedor se cae ("Address already in use").
/// <para>
/// MongoDB 7.0 y no 8.0: desde mediados de la serie 8.0, <c>mongod</c> se niega a arrancar en kernels Linux 6.19 o
/// posteriores (SERVER-121912), como los de los runners de GitHub Actions. Es la misma versión del <c>docker-compose.yml</c>.
/// </para>
/// </remarks>
public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _contenedor = new MongoDbBuilder("mongo:7.0")
        .WithUsername(string.Empty)
        .WithPassword(string.Empty)
        .Build();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public Task InitializeAsync() => _contenedor.StartAsync();

    public Task DisposeAsync() => _contenedor.DisposeAsync().AsTask();
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionMongo : ICollectionFixture<MongoFixture>
{
    public const string Nombre = "Mongo";
}

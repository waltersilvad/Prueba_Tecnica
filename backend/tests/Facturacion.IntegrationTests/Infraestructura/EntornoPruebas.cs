using Facturacion.Application;
using Facturacion.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using MongoDB.Driver;

namespace Facturacion.IntegrationTests.Infraestructura;

/// <summary>
/// Compone la aplicación con la misma configuración de DI de producción (<c>AddApplication</c> +
/// <c>AddInfrastructure</c>), apuntando a una base de datos y una carpeta de correos exclusivas de la prueba.
/// </summary>
internal sealed class EntornoPruebas : IAsyncDisposable
{
    public const string IdCliente1 = "6716a1000000000000000001";
    public const string IdCliente2 = "6716a1000000000000000002";
    public const string IdCliente3 = "6716a1000000000000000003";

    private readonly ServiceProvider _servicios;
    private readonly AsyncServiceScope _alcance;

    private EntornoPruebas(MongoFixture mongo, string entorno)
    {
        BaseDatos = $"pruebas_{Guid.NewGuid():N}";
        CarpetaCorreos = Path.Combine(Path.GetTempPath(), "facturacion-pruebas", Guid.NewGuid().ToString("N"));

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:CadenaConexion"] = mongo.CadenaConexion,
                ["Mongo:BaseDatos"] = BaseDatos,
                ["Email:Proveedor"] = "Consola",
                ["Email:Remitente"] = "avisos@example.com",
                ["Email:NombreRemitente"] = "Cartera - Pruebas",
                ["Email:CarpetaSalida"] = CarpetaCorreos,
                ["PlantillaCorreo:ZonaHoraria"] = "America/Bogota",
                ["PlantillaCorreo:Cultura"] = "es-CO",
            })
            .Build();

        var servicios = new ServiceCollection();
        servicios.AddSingleton<IConfiguration>(configuracion);
        servicios.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = entorno,
            ApplicationName = "Facturacion.IntegrationTests",
            ContentRootPath = AppContext.BaseDirectory,
        });
        servicios.AddLogging();
        servicios.AddApplication();
        servicios.AddInfrastructure(configuracion);

        _servicios = servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _alcance = _servicios.CreateAsyncScope();
    }

    public string BaseDatos { get; }

    public string CarpetaCorreos { get; }

    public IMongoDatabase Base => Obtener<IMongoDatabase>();

    /// <summary>Crea el entorno y ejecuta los servicios de arranque (esquema, índices y seed).</summary>
    public static async Task<EntornoPruebas> CrearAsync(MongoFixture mongo, string entorno = "Development")
    {
        var entornoPruebas = new EntornoPruebas(mongo, entorno);
        await entornoPruebas.ArrancarAsync();
        return entornoPruebas;
    }

    public T Obtener<T>()
        where T : notnull => _alcance.ServiceProvider.GetRequiredService<T>();

    /// <summary>Ejecuta los <see cref="IHostedService"/> como lo haría el host al arrancar.</summary>
    public async Task ArrancarAsync()
    {
        foreach (var servicio in _servicios.GetServices<IHostedService>())
        {
            await servicio.StartAsync(CancellationToken.None);
        }
    }

    public string[] ArchivosDeCorreo() =>
        Directory.Exists(CarpetaCorreos) ? Directory.GetFiles(CarpetaCorreos, "*.eml") : [];

    public async ValueTask DisposeAsync()
    {
        await Obtener<IMongoClient>().DropDatabaseAsync(BaseDatos);
        await _alcance.DisposeAsync();
        await _servicios.DisposeAsync();

        if (Directory.Exists(CarpetaCorreos))
        {
            Directory.Delete(CarpetaCorreos, recursive: true);
        }
    }
}

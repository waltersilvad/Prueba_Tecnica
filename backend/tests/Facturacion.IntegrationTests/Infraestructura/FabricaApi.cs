using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Facturacion.IntegrationTests.Infraestructura;

/// <summary>
/// Levanta la API real en memoria (mismo <c>Program.cs</c>) contra el Mongo del contenedor,
/// con una base de datos y una carpeta de correos exclusivas.
/// </summary>
internal sealed class FabricaApi(MongoFixture mongo, string entorno = "Development") : WebApplicationFactory<Program>
{
    public string BaseDatos { get; } = $"api_{Guid.NewGuid():N}";

    public string CarpetaCorreos { get; } =
        Path.Combine(Path.GetTempPath(), "facturacion-api-pruebas", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(entorno);
        builder.UseSetting("Mongo:CadenaConexion", mongo.CadenaConexion);
        builder.UseSetting("Mongo:BaseDatos", BaseDatos);
        builder.UseSetting("Email:Proveedor", "Consola");
        builder.UseSetting("Email:CarpetaSalida", CarpetaCorreos);
    }

    public override async ValueTask DisposeAsync()
    {
        await Services.GetRequiredService<IMongoClient>().DropDatabaseAsync(BaseDatos);
        await base.DisposeAsync();

        if (Directory.Exists(CarpetaCorreos))
        {
            Directory.Delete(CarpetaCorreos, recursive: true);
        }
    }
}

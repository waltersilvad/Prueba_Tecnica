using Facturacion.Api.Configuracion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Facturacion.IntegrationTests.Api;

public sealed class ServiciosApiTests
{
    [Fact]
    public void AddApi_AplicaAlChequeoDeMongoElTiempoMaximoConfigurado()
    {
        using var proveedor = CrearProveedor("00:00:07");

        var registro = Assert.Single(
            proveedor.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations);

        Assert.Equal("mongo", registro.Name);
        Assert.Equal(TimeSpan.FromSeconds(7), registro.Timeout);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    public void AddApi_ConTiempoMaximoNoPositivo_FallaLaValidacionDeOpciones(string tiempoMaximo)
    {
        using var proveedor = CrearProveedor(tiempoMaximo);

        var error = Assert.Throws<OptionsValidationException>(
            () => proveedor.GetRequiredService<IOptions<OpcionesSalud>>().Value);

        Assert.Contains("Salud:TiempoMaximoMongo", error.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider CrearProveedor(string tiempoMaximoMongo)
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Salud:TiempoMaximoMongo"] = tiempoMaximoMongo,
                ["Cors:OrigenesPermitidos:0"] = "http://localhost:5173",
            })
            .Build();

        var servicios = new ServiceCollection();
        servicios.AddSingleton<IConfiguration>(configuracion);
        servicios.AddApi(configuracion);
        return servicios.BuildServiceProvider();
    }
}

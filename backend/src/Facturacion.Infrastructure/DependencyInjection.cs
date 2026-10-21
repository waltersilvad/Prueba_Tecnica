using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Correo;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Clientes;
using Facturacion.Infrastructure.Comun;
using Facturacion.Infrastructure.Correo;
using Facturacion.Infrastructure.Persistencia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Facturacion.Infrastructure;

/// <summary>
/// Registro de las implementaciones de infraestructura (MongoDB, correo, reloj y bloqueo).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra los adaptadores de los puertos de la aplicación y valida su configuración al arrancar.
    /// El proveedor de correo se elige con <c>Email:Proveedor</c> (<c>Consola</c> o <c>Brevo</c>).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IReloj, RelojSistema>();
        services.AddSingleton<IBloqueoEjecucion, BloqueoEjecucionEnMemoria>();
        services.AddSingleton<IGeneradorId, GeneradorIdMongo>();

        AgregarMongo(services);
        AgregarCorreo(services, configuration);

        return services;
    }

    private static void AgregarMongo(IServiceCollection services)
    {
        services
            .AddOptions<OpcionesMongo>()
            .BindConfiguration(OpcionesMongo.Seccion)
            .Validate(o => !string.IsNullOrWhiteSpace(o.CadenaConexion), "Mongo:CadenaConexion es obligatoria.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseDatos), "Mongo:BaseDatos es obligatoria.")
            .ValidateOnStart();

        // El cliente de Mongo es seguro entre hilos y mantiene el pool de conexiones: una sola instancia.
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<OpcionesMongo>>().Value.CadenaConexion));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>()
            .GetDatabase(sp.GetRequiredService<IOptions<OpcionesMongo>>().Value.BaseDatos));
        services.AddSingleton<ContextoMongo>();

        services.AddSingleton<IClienteRepository, MongoClienteRepository>();
        services.AddSingleton<IFacturaRepository, MongoFacturaRepository>();
        services.AddSingleton<IEnvioRepository, MongoEnvioRepository>();

        services.AddSingleton<EsquemaMongo>();
        services.AddSingleton<DatosPrueba>();
        services.AddSingleton<IDatosPrueba>(sp => sp.GetRequiredService<DatosPrueba>());
        services.AddHostedService<InicializadorMongo>();
    }

    private static void AgregarCorreo(IServiceCollection services, IConfiguration configuration)
    {
        var opcionesEmail = services
            .AddOptions<OpcionesEmail>()
            .BindConfiguration(OpcionesEmail.Seccion)
            .Validate(o => Enum.IsDefined(o.Proveedor), "Email:Proveedor debe ser Consola o Brevo.")
            .Validate(o => Cliente.EsEmailValido(o.Remitente), "Email:Remitente debe ser un correo válido.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.NombreRemitente), "Email:NombreRemitente es obligatorio.");

        // El proveedor decide qué implementación se registra, así que se lee al componer los servicios.
        var proveedor = configuration.GetSection(OpcionesEmail.Seccion).GetValue<ProveedorEmail>(nameof(OpcionesEmail.Proveedor));

        if (proveedor == ProveedorEmail.Brevo)
        {
            opcionesEmail.ValidateOnStart();
            services
                .AddOptions<OpcionesSmtp>()
                .BindConfiguration(OpcionesSmtp.Seccion)
                .Validate(o => !string.IsNullOrWhiteSpace(o.Servidor), "Smtp:Servidor es obligatorio con Brevo.")
                .Validate(o => o.Puerto is > 0 and <= 65535, "Smtp:Puerto debe estar entre 1 y 65535.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.Usuario), "Smtp:Usuario es obligatorio con Brevo (usa user secrets o .env).")
                .Validate(o => !string.IsNullOrWhiteSpace(o.Clave), "Smtp:Clave es obligatoria con Brevo (usa user secrets o .env).")
                .Validate(o => o.Reintentos >= 0, "Smtp:Reintentos no puede ser negativo.")
                .Validate(o => o.EsperaEntreReintentos >= TimeSpan.Zero, "Smtp:EsperaEntreReintentos no puede ser negativa.")
                .ValidateOnStart();
            services.AddSingleton<IEmailSender, BrevoSmtpEmailSender>();
        }
        else
        {
            opcionesEmail
                .Validate(o => !string.IsNullOrWhiteSpace(o.CarpetaSalida), "Email:CarpetaSalida es obligatoria con Consola.")
                .ValidateOnStart();
            services.AddSingleton<IEmailSender, ConsolaEmailSender>();
        }
    }
}

using Facturacion.Api.Solicitudes;
using Facturacion.Infrastructure.Persistencia;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Facturacion.Api.Configuracion;

/// <summary>
/// Registro de los servicios propios de la capa HTTP.
/// </summary>
public static class ServiciosApi
{
    public const string PoliticaCors = "Frontend";

    private const string ChequeoMongo = "mongo";

    /// <summary>
    /// Registra ProblemDetails, CORS, validación, OpenAPI y health checks.
    /// </summary>
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddValidatorsFromAssemblyContaining<ClienteSolicitudValidator>(ServiceLifetime.Singleton);

        AgregarCors(services, configuration);
        AgregarSalud(services);

        services.AddOpenApi(opciones => opciones.AddDocumentTransformer((documento, _, _) =>
        {
            documento.Info = new OpenApiInfo
            {
                Title = "API de recordatorios de facturas",
                Version = "v1",
                Description =
                    "Envía recordatorios de cobro según el estado de cada factura " +
                    "(primerrecordatorio → segundorecordatorio → desactivado) y expone clientes, facturas, " +
                    "resumen por cliente e historial de envíos.",
            };
            return Task.CompletedTask;
        }));

        return services;
    }

    private static void AgregarCors(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OpcionesCors>()
            .BindConfiguration(OpcionesCors.Seccion)
            .Validate(o => o.OrigenesPermitidos.Length > 0, "Cors:OrigenesPermitidos debe tener al menos un origen.")
            .ValidateOnStart();

        // La política se construye al componer los servicios, por eso se lee la configuración aquí.
        var origenes = configuration.GetSection(OpcionesCors.Seccion).Get<OpcionesCors>()?.OrigenesPermitidos ?? [];
        services.AddCors(cors => cors.AddPolicy(PoliticaCors, politica => politica
            .WithOrigins(origenes)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
            .WithHeaders("Content-Type")));
    }

    private static void AgregarSalud(IServiceCollection services)
    {
        services
            .AddOptions<OpcionesSalud>()
            .BindConfiguration(OpcionesSalud.Seccion)
            .Validate(o => o.TiempoMaximoMongo > TimeSpan.Zero, "Salud:TiempoMaximoMongo debe ser mayor que cero.")
            .ValidateOnStart();

        services.AddHealthChecks().AddCheck<ChequeoSaludMongo>(ChequeoMongo);

        // El tiempo máximo se toma de las opciones ya validadas, no de la configuración cruda al componer los servicios.
        services
            .AddOptions<HealthCheckServiceOptions>()
            .Configure<IOptions<OpcionesSalud>>((opciones, salud) =>
                opciones.Registrations.Single(r => r.Name == ChequeoMongo).Timeout = salud.Value.TiempoMaximoMongo);
    }
}

using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Correo;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Application.Recordatorios;
using Facturacion.Application.Resumen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Facturacion.Application;

/// <summary>
/// Registro de los servicios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra casos de uso, plantillas de correo y sus opciones (validadas al arrancar).
    /// Los puertos (repositorios, correo, reloj, bloqueo) los registra la capa de infraestructura.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddOptions<OpcionesPlantillaCorreo>()
            .BindConfiguration(OpcionesPlantillaCorreo.Seccion)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<OpcionesPlantillaCorreo>, ValidadorOpcionesPlantillaCorreo>();
        services.AddSingleton<IPlantillaCorreo, PlantillaCorreoAvisos>();
        services.AddSingleton<CalendarioNegocio>();

        services.AddScoped<EnviadorAvisoFactura>();
        services.AddScoped<ProcesarRecordatoriosUseCase>();
        services.AddScoped<ConsultarClientesUseCase>();
        services.AddScoped<CrearClienteUseCase>();
        services.AddScoped<ActualizarClienteUseCase>();
        services.AddScoped<EliminarClienteUseCase>();
        services.AddScoped<ConsultarFacturasUseCase>();
        services.AddScoped<RegistrarFacturaUseCase>();
        services.AddScoped<EliminarFacturasUseCase>();
        services.AddScoped<ObtenerResumenClientesUseCase>();
        services.AddScoped<ConsultarEnviosUseCase>();

        return services;
    }
}

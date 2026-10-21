using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Al arrancar: aplica esquema e índices y, en Development, carga el seed si la base está vacía.
/// Es idempotente: se puede ejecutar en cada inicio sin duplicar datos.
/// </summary>
internal sealed partial class InicializadorMongo(
    EsquemaMongo esquema,
    DatosPrueba datosPrueba,
    IHostEnvironment entorno,
    ILogger<InicializadorMongo> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await esquema.AsegurarAsync(cancellationToken);
        LogEsquemaAplicado(logger);

        if (entorno.IsDevelopment() && await datosPrueba.SembrarSiVaciaAsync(cancellationToken))
        {
            LogSemillaCargada(logger, DatosSemilla.Clientes.Count, DatosSemilla.Facturas.Count);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Esquema e índices de MongoDB aplicados.")]
    private static partial void LogEsquemaAplicado(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Datos de prueba cargados: {Clientes} clientes y {Facturas} facturas.")]
    private static partial void LogSemillaCargada(ILogger logger, int clientes, int facturas);
}

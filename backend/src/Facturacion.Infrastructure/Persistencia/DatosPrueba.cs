using Facturacion.Application.Comun;
using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;
using Facturacion.Infrastructure.Correo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Carga y borra los datos de prueba. Comparte el bloqueo del proceso de avisos: nunca se borra ni se carga a mitad
/// de un envío.
/// </summary>
internal sealed partial class DatosPrueba(
    ContextoMongo contexto,
    IReloj reloj,
    IBloqueoEjecucion bloqueo,
    IOptions<OpcionesEmail> opcionesEmail,
    ILogger<DatosPrueba> logger) : IDatosPrueba
{
    private const string PatronCorreos = "*.eml";

    /// <summary>Al arrancar: carga el ejemplo solo si la base no tiene clientes ni facturas.</summary>
    /// <returns><see langword="true"/> si se cargaron datos.</returns>
    public async Task<bool> SembrarSiVaciaAsync(CancellationToken cancellationToken)
    {
        if (!await EstaVaciaAsync(cancellationToken))
        {
            return false;
        }

        await InsertarEjemploAsync(cancellationToken);
        return true;
    }

    public async Task<Result<ResumenBorrado>> BorrarTodoAsync(CancellationToken cancellationToken)
    {
        using var bloqueoAdquirido = bloqueo.IntentarAdquirir();
        if (bloqueoAdquirido is null)
        {
            return Result.Fallo<ResumenBorrado>(ErroresAplicacion.EjecucionEnCurso);
        }

        // Primero lo que depende de los clientes, igual que al eliminar un cliente.
        var envios = await contexto.Envios.DeleteManyAsync(FilterDefinition<EnvioDocumento>.Empty, cancellationToken);
        var facturas = await contexto.Facturas.DeleteManyAsync(FilterDefinition<FacturaDocumento>.Empty, cancellationToken);
        var clientes = await contexto.Clientes.DeleteManyAsync(FilterDefinition<ClienteDocumento>.Empty, cancellationToken);
        var correos = BorrarCorreosGuardados();

        return Result.Exito(new ResumenBorrado(clientes.DeletedCount, facturas.DeletedCount, envios.DeletedCount, correos));
    }

    public async Task<Result<ResumenCargaEjemplo>> CargarEjemploAsync(CancellationToken cancellationToken)
    {
        using var bloqueoAdquirido = bloqueo.IntentarAdquirir();
        if (bloqueoAdquirido is null)
        {
            return Result.Fallo<ResumenCargaEjemplo>(ErroresAplicacion.EjecucionEnCurso);
        }

        if (!await EstaVaciaAsync(cancellationToken))
        {
            return Result.Fallo<ResumenCargaEjemplo>(ErroresDatosPrueba.BaseConDatos);
        }

        await InsertarEjemploAsync(cancellationToken);
        return Result.Exito(new ResumenCargaEjemplo(DatosSemilla.Clientes.Count, DatosSemilla.Facturas.Count));
    }

    private async Task<bool> EstaVaciaAsync(CancellationToken cancellationToken) =>
        !await contexto.Clientes.Find(FilterDefinition<ClienteDocumento>.Empty).AnyAsync(cancellationToken)
        && !await contexto.Facturas.Find(FilterDefinition<FacturaDocumento>.Empty).AnyAsync(cancellationToken)
        && !await contexto.Envios.Find(FilterDefinition<EnvioDocumento>.Empty).AnyAsync(cancellationToken);

    private async Task InsertarEjemploAsync(CancellationToken cancellationToken)
    {
        var ahora = reloj.AhoraUtc;

        await contexto.Clientes.InsertManyAsync(
            DatosSemilla.Clientes.Select(c => new ClienteDocumento
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Email = c.Email,
                FechaActualizacion = ahora,
            }),
            cancellationToken: cancellationToken);

        await contexto.Facturas.InsertManyAsync(
            DatosSemilla.Facturas.Select(f => new FacturaDocumento
            {
                Id = f.Id,
                Numero = f.Numero,
                ClienteId = f.ClienteId,
                Monto = f.Monto,
                Moneda = DatosSemilla.Moneda,
                FechaEmision = f.FechaEmision,
                FechaVencimiento = f.FechaVencimiento,
                Estado = f.Estado.ATexto(),
                FechaActualizacion = ahora,
            }),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Borra solo los archivos <c>.eml</c> de la carpeta de salida configurada (sin subcarpetas ni otros archivos).
    /// Un archivo que no se puede borrar (por ejemplo, abierto en otro programa) se omite y queda en el log.
    /// </summary>
    private int BorrarCorreosGuardados()
    {
        var carpetaConfigurada = opcionesEmail.Value.CarpetaSalida;
        if (string.IsNullOrWhiteSpace(carpetaConfigurada))
        {
            return 0;
        }

        var carpeta = Path.GetFullPath(carpetaConfigurada);
        if (!Directory.Exists(carpeta))
        {
            return 0;
        }

        var borrados = 0;
        foreach (var archivo in Directory.EnumerateFiles(carpeta, PatronCorreos, SearchOption.TopDirectoryOnly))
        {
            try
            {
                File.Delete(archivo);
                borrados++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LogCorreoNoBorrado(logger, ex, archivo);
            }
        }

        return borrados;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo borrar el correo guardado {Archivo}.")]
    private static partial void LogCorreoNoBorrado(ILogger logger, Exception ex, string archivo);
}

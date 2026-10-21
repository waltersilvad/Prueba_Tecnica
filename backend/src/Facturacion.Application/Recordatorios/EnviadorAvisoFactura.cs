using Facturacion.Application.Comun;
using Facturacion.Application.Correo;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Clientes;
using Facturacion.Domain.Facturas;
using Microsoft.Extensions.Logging;

namespace Facturacion.Application.Recordatorios;

/// <summary>
/// Procesa el aviso de una sola factura: genera y envía el correo, registra el intento en el historial y, solo si el
/// correo salió, avanza el estado con una actualización condicional.
/// </summary>
/// <remarks>
/// Es la regla común de todas las formas de procesar (masivo, una factura o una selección). Cualquier fallo queda
/// aislado en esta factura y se informa en el acumulador del cliente; la cancelación sí se propaga.
/// </remarks>
public sealed partial class EnviadorAvisoFactura(
    IFacturaRepository facturas,
    IEnvioRepository envios,
    IEmailSender emailSender,
    IPlantillaCorreo plantilla,
    IReloj reloj,
    ILogger<EnviadorAvisoFactura> logger)
{
    internal async Task ProcesarAsync(ContextoEnvio contexto, CancellationToken cancellationToken)
    {
        var estadoAnterior = contexto.Factura.Estado;

        if (!await EnviarAvisoAsync(contexto, cancellationToken))
        {
            return;
        }

        try
        {
            // No puede fallar: DeterminarAviso() ya confirmó que la factura está en un estado procesable.
            _ = contexto.Factura.AvanzarEstado(reloj.AhoraUtc);

            if (await facturas.ActualizarEstadoAsync(contexto.Factura, estadoAnterior, cancellationToken))
            {
                contexto.Acumulador.ContarFacturaActualizada();
            }
            else
            {
                LogConflicto(logger, contexto.Factura.Numero);
                contexto.Acumulador.RegistrarConflicto(contexto.Factura.Numero);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogErrorActualizando(logger, ex, contexto.Factura.Numero);
            contexto.Acumulador.RegistrarError(
                $"Factura {contexto.Factura.Numero}: el aviso se envió pero no se pudo actualizar el estado ({ex.Message}).");
        }
    }

    /// <returns><see langword="true"/> si el correo salió.</returns>
    private async Task<bool> EnviarAvisoAsync(ContextoEnvio contexto, CancellationToken cancellationToken)
    {
        try
        {
            var contenido = plantilla.Generar(contexto.Aviso, contexto.Cliente, contexto.Factura);
            var mensaje = new MensajeCorreo(
                contexto.Cliente.Email,
                contexto.Cliente.Nombre,
                contenido.Asunto,
                contenido.CuerpoHtml,
                contenido.CuerpoTexto);

            await emailSender.EnviarAsync(mensaje, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogErrorEnviando(logger, ex, contexto.Factura.Numero);
            contexto.Acumulador.RegistrarError(
                $"Factura {contexto.Factura.Numero}: no se pudo enviar el aviso ({ex.Message}).");
            await RegistrarEnvioAsync(contexto, ResultadoEnvio.Fallido, ex.Message, cancellationToken);
            return false;
        }

        contexto.Acumulador.ContarCorreoEnviado();
        await RegistrarEnvioAsync(contexto, ResultadoEnvio.Enviado, null, cancellationToken);
        return true;
    }

    /// <remarks>
    /// Se registra justo después de intentar el envío (antes de actualizar el estado) para que la auditoría
    /// refleje que el correo salió aunque la actualización falle después. Un fallo al registrar no detiene el proceso.
    /// </remarks>
    private async Task RegistrarEnvioAsync(
        ContextoEnvio contexto, ResultadoEnvio resultado, string? error, CancellationToken cancellationToken)
    {
        var registro = new RegistroEnvio(
            contexto.EjecucionId,
            contexto.Cliente.Id,
            contexto.Factura.Id,
            contexto.Aviso,
            contexto.Cliente.Email,
            reloj.AhoraUtc,
            resultado,
            error);

        try
        {
            await envios.RegistrarAsync(registro, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogErrorRegistrandoEnvio(logger, ex, contexto.Factura.Numero);
            contexto.Acumulador.RegistrarError(
                $"Factura {contexto.Factura.Numero}: no se pudo registrar el envío en el historial ({ex.Message}).");
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo enviar el aviso de la factura {Numero}.")]
    private static partial void LogErrorEnviando(ILogger logger, Exception ex, string numero);

    [LoggerMessage(Level = LogLevel.Error, Message = "Aviso enviado pero no se actualizó el estado de la factura {Numero}.")]
    private static partial void LogErrorActualizando(ILogger logger, Exception ex, string numero);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo registrar el envío de la factura {Numero}.")]
    private static partial void LogErrorRegistrandoEnvio(ILogger logger, Exception ex, string numero);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Conflicto: otra ejecución cambió el estado de la factura {Numero}.")]
    private static partial void LogConflicto(ILogger logger, string numero);
}

/// <summary>Datos de un aviso en curso: a qué ejecución pertenece, a quién va y dónde se acumula su resultado.</summary>
internal sealed record ContextoEnvio(
    string EjecucionId,
    Cliente Cliente,
    Factura Factura,
    TipoAviso Aviso,
    AcumuladorResultadoCliente Acumulador);

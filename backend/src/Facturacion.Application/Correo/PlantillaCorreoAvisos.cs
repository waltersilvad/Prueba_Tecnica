using System.Globalization;
using System.Net;
using Facturacion.Domain.Clientes;
using Facturacion.Domain.Facturas;
using Microsoft.Extensions.Options;

namespace Facturacion.Application.Correo;

/// <summary>
/// Plantillas de los avisos de cobro. Cada aviso tiene su asunto y mensaje; la estructura es común.
/// </summary>
/// <remarks>
/// Los datos del cliente se escapan en el HTML para evitar inyección de marcado en el correo.
/// </remarks>
public sealed class PlantillaCorreoAvisos : IPlantillaCorreo
{
    private readonly TimeZoneInfo _zonaHoraria;
    private readonly CultureInfo _cultura;

    public PlantillaCorreoAvisos(IOptions<OpcionesPlantillaCorreo> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(opciones.Value.ZonaHoraria);
        _cultura = CultureInfo.GetCultureInfo(opciones.Value.Cultura);
    }

    public ContenidoCorreo Generar(TipoAviso aviso, Cliente cliente, Factura factura)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentNullException.ThrowIfNull(factura);

        var textos = TextosDe(aviso, factura.Numero);
        var datos = new DatosFactura(
            factura.Numero,
            string.Create(_cultura, $"{factura.Monto:N2} {factura.Moneda}"),
            TimeZoneInfo.ConvertTimeFromUtc(factura.FechaVencimiento, _zonaHoraria).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

        return new ContenidoCorreo(
            textos.Asunto,
            GenerarHtml(textos, cliente.Nombre, datos),
            GenerarTexto(textos, cliente.Nombre, datos));
    }

    private static TextosAviso TextosDe(TipoAviso aviso, string numero) => aviso switch
    {
        TipoAviso.SegundoRecordatorio => new TextosAviso(
            $"Segundo recordatorio: factura {numero} pendiente de pago",
            "Te recordamos por segunda vez que la siguiente factura sigue pendiente de pago.",
            "Si ya realizaste el pago, por favor ignora este mensaje."),
        TipoAviso.Desactivacion => new TextosAviso(
            $"Aviso de desactivación: factura {numero}",
            "No hemos recibido el pago de la siguiente factura después de dos recordatorios, por lo que ha sido desactivada.",
            "Comunícate con nosotros para regularizar tu situación."),
        _ => throw new ArgumentOutOfRangeException(nameof(aviso), aviso, "Tipo de aviso no soportado."),
    };

    private static string GenerarTexto(TextosAviso textos, string nombreCliente, DatosFactura datos) =>
        $"""
        Hola {nombreCliente},

        {textos.Mensaje}

        Factura: {datos.Numero}
        Monto: {datos.Monto}
        Fecha de vencimiento: {datos.FechaVencimiento}

        {textos.Cierre}
        """;

    private static string GenerarHtml(TextosAviso textos, string nombreCliente, DatosFactura datos)
    {
        static string Html(string valor) => WebUtility.HtmlEncode(valor);

        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <body style="font-family: Arial, sans-serif; color: #1f2937;">
              <p>Hola {Html(nombreCliente)},</p>
              <p>{Html(textos.Mensaje)}</p>
              <table style="border-collapse: collapse;">
                <tr><td style="padding: 4px 12px 4px 0;"><strong>Factura</strong></td><td>{Html(datos.Numero)}</td></tr>
                <tr><td style="padding: 4px 12px 4px 0;"><strong>Monto</strong></td><td>{Html(datos.Monto)}</td></tr>
                <tr><td style="padding: 4px 12px 4px 0;"><strong>Fecha de vencimiento</strong></td><td>{Html(datos.FechaVencimiento)}</td></tr>
              </table>
              <p>{Html(textos.Cierre)}</p>
            </body>
            </html>
            """;
    }

    private sealed record TextosAviso(string Asunto, string Mensaje, string Cierre);

    private sealed record DatosFactura(string Numero, string Monto, string FechaVencimiento);
}

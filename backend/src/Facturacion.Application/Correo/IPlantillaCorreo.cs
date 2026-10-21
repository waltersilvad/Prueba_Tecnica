using Facturacion.Domain.Clientes;
using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Correo;

/// <summary>
/// Genera el contenido del correo de cada tipo de aviso.
/// </summary>
public interface IPlantillaCorreo
{
    /// <summary>Genera asunto, cuerpo HTML y cuerpo de texto plano para el aviso de una factura.</summary>
    ContenidoCorreo Generar(TipoAviso aviso, Cliente cliente, Factura factura);
}

/// <summary>Contenido de un correo, sin destinatario.</summary>
public sealed record ContenidoCorreo(string Asunto, string CuerpoHtml, string CuerpoTexto);

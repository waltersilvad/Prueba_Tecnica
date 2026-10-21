using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Clientes;
using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;
using MongoDB.Bson;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Conversión explícita entre documentos de Mongo y entidades del dominio.
/// </summary>
internal static class MapeoDocumentos
{
    /// <exception cref="InvalidOperationException">Si el documento no cumple las reglas del dominio.</exception>
    public static Cliente ACliente(ClienteDocumento documento)
    {
        var cliente = Cliente.Crear(documento.Id.ToString(), documento.Nombre, documento.Email, documento.FechaActualizacion);
        return cliente.EsExitoso
            ? cliente.Valor
            : throw new InvalidOperationException(
                $"El cliente {documento.Id} almacenado no es válido: {cliente.Error.Mensaje}");
    }

    /// <summary>
    /// Convierte el documento en factura, o devuelve el motivo por el que no se puede interpretar.
    /// </summary>
    public static Result<Factura> AFactura(FacturaDocumento documento)
    {
        var estado = EstadoFacturaTextos.DesdeTexto(documento.Estado);
        if (estado.EsFallido)
        {
            return Result.Fallo<Factura>(estado.Error);
        }

        return Factura.Crear(
            documento.Id.ToString(),
            documento.Numero,
            documento.ClienteId.ToString(),
            documento.Monto,
            documento.Moneda,
            documento.FechaEmision,
            documento.FechaVencimiento,
            estado.Valor,
            documento.FechaActualizacion);
    }

    public static LecturaFacturas ALectura(IEnumerable<FacturaDocumento> documentos)
    {
        var facturas = new List<Factura>();
        var noInterpretables = new List<FacturaNoInterpretable>();

        foreach (var documento in documentos)
        {
            var factura = AFactura(documento);
            if (factura.EsExitoso)
            {
                facturas.Add(factura.Valor);
            }
            else
            {
                noInterpretables.Add(new FacturaNoInterpretable(documento.Id.ToString(), documento.Numero, factura.Error));
            }
        }

        return new LecturaFacturas(facturas, noInterpretables);
    }

    public static ClienteDocumento ADocumento(Cliente cliente) => new()
    {
        Id = ObjectId.Parse(cliente.Id),
        Nombre = cliente.Nombre,
        Email = cliente.Email,
        FechaActualizacion = cliente.FechaActualizacion,
    };

    public static FacturaDocumento ADocumento(Factura factura) => new()
    {
        Id = ObjectId.Parse(factura.Id),
        Numero = factura.Numero,
        ClienteId = ObjectId.Parse(factura.ClienteId),
        Monto = factura.Monto,
        Moneda = factura.Moneda,
        FechaEmision = factura.FechaEmision,
        FechaVencimiento = factura.FechaVencimiento,
        Estado = factura.Estado.ATexto(),
        FechaActualizacion = factura.FechaActualizacion,
    };

    public static EnvioDocumento ADocumento(RegistroEnvio registro) => new()
    {
        Id = ObjectId.GenerateNewId(),
        EjecucionId = registro.EjecucionId,
        ClienteId = ObjectId.Parse(registro.ClienteId),
        FacturaId = ObjectId.Parse(registro.FacturaId),
        TipoAviso = registro.TipoAviso.ToString(),
        Destinatario = registro.Destinatario,
        Fecha = registro.Fecha,
        Resultado = registro.Resultado.ToString(),
        Error = registro.Error,
    };

    public static RegistroEnvio ARegistro(EnvioDocumento documento) => new(
        documento.EjecucionId,
        documento.ClienteId.ToString(),
        documento.FacturaId.ToString(),
        Enum.Parse<TipoAviso>(documento.TipoAviso),
        documento.Destinatario,
        documento.Fecha,
        Enum.Parse<ResultadoEnvio>(documento.Resultado),
        documento.Error);
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Facturacion.Infrastructure.Persistencia;

// Modelos de persistencia: reflejan exactamente los documentos de Mongo (campos en camelCase) y se
// convierten de forma explícita a las entidades del dominio, que no conocen a Mongo.

[BsonIgnoreExtraElements]
internal sealed class ClienteDocumento
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("fechaActualizacion")]
    public DateTime FechaActualizacion { get; set; }
}

[BsonIgnoreExtraElements]
internal sealed class FacturaDocumento
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("numero")]
    public string Numero { get; set; } = string.Empty;

    [BsonElement("clienteId")]
    public ObjectId ClienteId { get; set; }

    [BsonElement("monto")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Monto { get; set; }

    [BsonElement("moneda")]
    public string Moneda { get; set; } = string.Empty;

    [BsonElement("fechaEmision")]
    public DateTime FechaEmision { get; set; }

    [BsonElement("fechaVencimiento")]
    public DateTime FechaVencimiento { get; set; }

    /// <summary>Texto exacto del negocio: <c>primerrecordatorio</c>, <c>segundorecordatorio</c> o <c>desactivado</c>.</summary>
    [BsonElement("estado")]
    public string Estado { get; set; } = string.Empty;

    [BsonElement("fechaActualizacion")]
    public DateTime FechaActualizacion { get; set; }
}

[BsonIgnoreExtraElements]
internal sealed class EnvioDocumento
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("ejecucionId")]
    public string EjecucionId { get; set; } = string.Empty;

    [BsonElement("clienteId")]
    public ObjectId ClienteId { get; set; }

    [BsonElement("facturaId")]
    public ObjectId FacturaId { get; set; }

    [BsonElement("tipoAviso")]
    public string TipoAviso { get; set; } = string.Empty;

    [BsonElement("destinatario")]
    public string Destinatario { get; set; } = string.Empty;

    [BsonElement("fecha")]
    public DateTime Fecha { get; set; }

    [BsonElement("resultado")]
    public string Resultado { get; set; } = string.Empty;

    [BsonElement("error")]
    public string? Error { get; set; }
}

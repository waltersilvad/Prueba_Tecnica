namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Conexión a MongoDB (sección <c>Mongo</c>).
/// </summary>
public sealed class OpcionesMongo
{
    public const string Seccion = "Mongo";

    /// <summary>Cadena de conexión. Ejemplo: <c>mongodb://localhost:27017</c>.</summary>
    public string CadenaConexion { get; set; } = string.Empty;

    /// <summary>Nombre de la base de datos.</summary>
    public string BaseDatos { get; set; } = string.Empty;
}

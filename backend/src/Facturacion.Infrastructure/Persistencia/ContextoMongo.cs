using MongoDB.Driver;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Acceso tipado a las colecciones de la base <c>facturacion</c>.
/// </summary>
internal sealed class ContextoMongo(IMongoDatabase baseDatos)
{
    public const string ColeccionClientes = "clientes";
    public const string ColeccionFacturas = "facturas";
    public const string ColeccionEnvios = "envios";

    public IMongoDatabase BaseDatos => baseDatos;

    public IMongoCollection<ClienteDocumento> Clientes =>
        baseDatos.GetCollection<ClienteDocumento>(ColeccionClientes);

    public IMongoCollection<FacturaDocumento> Facturas =>
        baseDatos.GetCollection<FacturaDocumento>(ColeccionFacturas);

    public IMongoCollection<EnvioDocumento> Envios =>
        baseDatos.GetCollection<EnvioDocumento>(ColeccionEnvios);
}

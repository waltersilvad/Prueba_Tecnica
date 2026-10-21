using Facturacion.Application.Comun;
using MongoDB.Bson;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>Genera ids con formato <see cref="ObjectId"/>, el tipo de <c>_id</c> de las colecciones.</summary>
internal sealed class GeneradorIdMongo : IGeneradorId
{
    public string NuevoId() => ObjectId.GenerateNewId().ToString();
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Verifica que MongoDB responda con un comando <c>ping</c>.
/// </summary>
public sealed class ChequeoSaludMongo(IMongoDatabase baseDatos) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await baseDatos.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB responde.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("MongoDB no responde.", ex);
        }
    }
}

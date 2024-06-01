using System.Collections;
using System.Collections.ObjectModel;
using IBM.WMQ;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.IbmMQ;

/// <summary>
/// A health check for IBM MQ services.
/// </summary>
#if NETSTANDARD2_0
[Obsolete("Use .NET6 based MQ Client libraries", false)]
#endif
public class IbmMQHealthCheck : IHealthCheck
{
    private readonly Hashtable _connectionProperties;
    private readonly string _queueManager;
    private readonly Dictionary<string, object> _baseCheckDetails = new Dictionary<string, object>{
                    { "healthcheck.name", nameof(IbmMQHealthCheck) },
                    { "healthcheck.task", "ready" },
                    { "messaging.system", "ibmmq" },
                    { "event.name", "messaging.healthcheck"}
    };

    public IbmMQHealthCheck(string queueManager, Hashtable connectionProperties)
    {
        Guard.ThrowIfNull(queueManager, true);

        _queueManager = queueManager;
        _connectionProperties = Guard.ThrowIfNull(connectionProperties);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> checkDetails = _baseCheckDetails;
        try
        {
            checkDetails.Add("server.address", _connectionProperties["hostname"] ?? "");
            checkDetails.Add("server.port", _connectionProperties["port"] ?? "");
            using var connection = new MQQueueManager(_queueManager, _connectionProperties);
            return HealthCheckResult.Healthy(data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex, data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
    }
}

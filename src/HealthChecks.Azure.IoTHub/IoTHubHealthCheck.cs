using System.Collections.ObjectModel;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.Azure.IoTHub;

public class IoTHubHealthCheck : IHealthCheck
{
    private readonly IoTHubOptions _options;
    private readonly Dictionary<string, object> _baseCheckDetails = new Dictionary<string, object>{
                    { "healthcheck.name", nameof(IoTHubHealthCheck) },
                    { "healthcheck.task", "ready" },
                    { "messaging.system", "azureiothub" },
                    { "event.name", "messaging.healthcheck"}
    };

    public IoTHubHealthCheck(IoTHubOptions options)
    {
        _options = Guard.ThrowIfNull(options);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> checkDetails = _baseCheckDetails;
        try
        {
            if (_options.RegistryWriteCheck)
            {
                await ExecuteRegistryWriteCheckAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (_options.RegistryReadCheck)
            {
                await ExecuteRegistryReadCheckAsync().ConfigureAwait(false);
            }
            if (_options.ServiceConnectionCheck)
            {
                await ExecuteServiceConnectionCheckAsync(cancellationToken).ConfigureAwait(false);
            }

            return HealthCheckResult.Healthy(data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex, data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
    }

    private async Task ExecuteServiceConnectionCheckAsync(CancellationToken cancellationToken)
    {
        using var client = ServiceClient.CreateFromConnectionString(_options.ConnectionString, _options.ServiceConnectionTransport);
        await client.GetServiceStatisticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteRegistryReadCheckAsync()
    {
        using var client = RegistryManager.CreateFromConnectionString(_options.ConnectionString);
        var query = client.CreateQuery(_options.RegistryReadQuery, 1);
        await query.GetNextAsJsonAsync().ConfigureAwait(false);
    }

    private async Task ExecuteRegistryWriteCheckAsync(CancellationToken cancellationToken)
    {
        using var client = RegistryManager.CreateFromConnectionString(_options.ConnectionString);

        var deviceId = _options.RegistryWriteDeviceIdFactory();
        var device = await client.GetDeviceAsync(deviceId, cancellationToken).ConfigureAwait(false);

        // in default implementation of configuration deviceId equals "health-check-registry-write-device-id"
        // if in previous health check device were not removed -- try remove it
        // if in previous health check device were added and removed -- try create and remove it
        if (device != null)
        {
            await client.RemoveDeviceAsync(deviceId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await client.AddDeviceAsync(new Device(deviceId), cancellationToken).ConfigureAwait(false);
            await client.RemoveDeviceAsync(deviceId, cancellationToken).ConfigureAwait(false);
        }
    }
}

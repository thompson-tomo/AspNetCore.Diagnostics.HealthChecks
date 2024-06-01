using System.Collections.ObjectModel;
using HealthChecks.AzureServiceBus.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.AzureServiceBus;

public class AzureServiceBusQueueHealthCheck : AzureServiceBusHealthCheck<AzureServiceBusQueueHealthCheckOptions>, IHealthCheck
{
    private readonly string _queueKey;
    private readonly Dictionary<string, object> _baseCheckDetails = new Dictionary<string, object>{
                    { "healthcheck.name", nameof(AzureServiceBusQueueHealthCheck) },
                    { "healthcheck.task", "queueready" },
                    { "messaging.system", "azurestoragebus" },
                    { "event.name", "messaging.healthcheck"}
    };

    public AzureServiceBusQueueHealthCheck(AzureServiceBusQueueHealthCheckOptions options, ServiceBusClientProvider clientProvider)
        : base(options, clientProvider)
    {
        Guard.ThrowIfNull(options.QueueName, true);

        _queueKey = $"{nameof(AzureServiceBusQueueHealthCheck)}_{ConnectionKey}_{Options.QueueName}";
    }

    public AzureServiceBusQueueHealthCheck(AzureServiceBusQueueHealthCheckOptions options)
        : this(options, new ServiceBusClientProvider())
    { }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> checkDetails = _baseCheckDetails;
        try
        {
            if (Options.UsePeekMode)
                await CheckWithReceiver().ConfigureAwait(false);
            else
                await CheckWithManagement().ConfigureAwait(false);

            return HealthCheckResult.Healthy(data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex, data: new ReadOnlyDictionary<string, object>(checkDetails));
        }

        async Task CheckWithReceiver()
        {
            var client = await ClientCache.GetOrAddAsyncDisposableAsync(ConnectionKey, _ => CreateClient()).ConfigureAwait(false);
            var receiver = await ClientCache.GetOrAddAsyncDisposableAsync(
                _queueKey,
                _ => client.CreateReceiver(Options.QueueName))
                .ConfigureAwait(false);

            await receiver.PeekMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        Task CheckWithManagement()
        {
            var managementClient = ClientCache.GetOrAdd(ConnectionKey, _ => CreateManagementClient());

            return managementClient.GetQueueRuntimePropertiesAsync(Options.QueueName, cancellationToken);
        }
    }
}

using System.Collections.ObjectModel;
using HealthChecks.AzureServiceBus.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.AzureServiceBus;

public class AzureServiceBusTopicHealthCheck : AzureServiceBusHealthCheck<AzureServiceBusTopicHealthCheckOptions>, IHealthCheck
{
    private readonly Dictionary<string, object> _baseCheckDetails = new Dictionary<string, object>{
                    { "healthcheck.name", nameof(AzureServiceBusTopicHealthCheck) },
                    { "healthcheck.task", "topicready" },
                    { "messaging.system", "azurestoragebus" },
                    { "event.name", "messaging.healthcheck"}
    };
    public AzureServiceBusTopicHealthCheck(AzureServiceBusTopicHealthCheckOptions options, ServiceBusClientProvider clientProvider)
        : base(options, clientProvider)
    {
        Guard.ThrowIfNull(options.TopicName, true);
    }

    public AzureServiceBusTopicHealthCheck(AzureServiceBusTopicHealthCheckOptions options)
        : this(options, new ServiceBusClientProvider())
    { }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> checkDetails = _baseCheckDetails;
        try
        {
            var managementClient = ClientCache.GetOrAdd(ConnectionKey, _ => CreateManagementClient());
            _ = await managementClient.GetTopicRuntimePropertiesAsync(Options.TopicName, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy(data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex, data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
    }
}

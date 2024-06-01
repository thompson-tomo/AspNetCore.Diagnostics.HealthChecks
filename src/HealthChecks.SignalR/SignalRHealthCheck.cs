using System.Collections.ObjectModel;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecks.SignalR;

public class SignalRHealthCheck : IHealthCheck
{
    private readonly Func<HubConnection> _hubConnectionBuilder;
    private readonly Dictionary<string, object> _baseCheckDetails = new Dictionary<string, object>{
                    { "healthcheck.name", nameof(SignalRHealthCheck) },
                    { "healthcheck.task", "ready" },
                    { "messaging.system", "signalr" },
                    { "event.name", "messaging.healthcheck"}
    };

    public SignalRHealthCheck(Func<HubConnection> hubConnectionBuilder)
    {
        _hubConnectionBuilder = Guard.ThrowIfNull(hubConnectionBuilder);
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        HubConnection? connection = null;
        Dictionary<string, object> checkDetails = _baseCheckDetails;

        try
        {
            connection = _hubConnectionBuilder();

            checkDetails.Add("signalr.connectionid", connection.ConnectionId ?? "");

            await connection.StartAsync(cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy(data: new ReadOnlyDictionary<string, object>(checkDetails));
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
        finally
        {
            if (connection != null)
                await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}

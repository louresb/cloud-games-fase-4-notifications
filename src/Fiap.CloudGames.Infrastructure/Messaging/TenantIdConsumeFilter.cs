using MassTransit;

namespace Fiap.CloudGames.Infrastructure.Messaging;

/// <summary>
/// MassTransit filter that pushes the <c>X-Tenant-Id</c> header (if present) into the
/// Serilog LogContext for the duration of the consume invocation. Every log line
/// emitted during the consumer run will carry the TenantId property automatically.
/// </summary>
public sealed class TenantIdConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public const string HeaderName = "X-Tenant-Id";
    public const string LogProperty = "TenantId";
    public const string Default = "unknown";

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        string tenant = Default;
        if (context.Headers.TryGetHeader(HeaderName, out var raw) && raw is string s && !string.IsNullOrWhiteSpace(s))
        {
            tenant = s;
        }

        using (Serilog.Context.LogContext.PushProperty(LogProperty, tenant))
        {
            await next.Send(context);
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("tenantIdEnrichment");
}

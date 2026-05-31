using Fiap.CloudGames.Application.Payments.Consumers;
using Fiap.CloudGames.Application.Users.Consumers;
using Fiap.CloudGames.Infrastructure;
using Fiap.CloudGames.Worker.Middlewares;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Serilog (Console + Loki)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Fiap.CloudGames.Notifications.Worker")
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        uri: builder.Configuration["Loki:Url"] ?? "http://localhost:3100",
        labels: new[]
        {
            new LokiLabel { Key = "service", Value = "notifications-svc" },
            new LokiLabel { Key = "env", Value = builder.Environment.EnvironmentName.ToLower() }
        }
    )
    .CreateLogger();

// Substitui o logger padrão do .NET pelo Serilog
builder.Host.UseSerilog();

// Configure infrastructure based on messaging provider
// Demo note: keeping provider selection explicit helps show RabbitMQ locally and SQS readiness on AWS.
var messagingProvider = builder.Configuration["MESSAGING_PROVIDER"] ?? "RabbitMQ";
Log.Information("Using messaging provider: {MessagingProvider}", messagingProvider);

if (messagingProvider.Equals("SQS", StringComparison.OrdinalIgnoreCase))
{
    // SQS mode for AWS ECS deployment
    builder.Services.AddInfrastructureSqs(builder.Configuration);
}
else
{
    // RabbitMQ mode for local/dev
    builder.Services.AddInfrastructure(
        builder.Configuration, 
        typeof(UserSignedUpConsumer).Assembly,
        consumerEventTypes:
        [
            typeof(UserSignedUpConsumer),
            typeof(UserEmailConfirmedConsumer),
            typeof(UserInvitedConsumer),
            typeof(UserFirstAccessedConsumer),
            typeof(UserForgotPasswordConsumer),
            typeof(UserPasswordResetedConsumer),
            typeof(PaymentLinkGeneratedConsumer),
            typeof(PaymentRefundedConsumer),
            typeof(PaymentSucceededConsumer),
        ]);
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();

// Liveness: Só diz que o processo está de pé
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: Diz se as dependências estão OK
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

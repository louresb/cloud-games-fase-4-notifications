using System.Reflection;
using Amazon.SQS;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Fiap.CloudGames.Infrastructure.Email.Services;
using Fiap.CloudGames.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration, 
        Assembly applicationAssembly,
        Type[]? consumerCommandTypes = null,
        Type[]? consumerEventTypes = null)
    {
        services.AddSingleton<IEmailService, ConsoleEmailService>();

        var notificationsCommandsQueue = configuration["Queues:Notifications:Commands"] ?? throw new InvalidOperationException("Notifications commands queue not configured.");
        var notificationsEventsQueue = configuration["Queues:Notifications:Events"] ?? throw new InvalidOperationException("Notifications events queue not configured.");

        consumerCommandTypes = consumerCommandTypes?.Where(t => typeof(IConsumer).IsAssignableFrom(t)).ToArray() ?? [];
        consumerEventTypes = consumerEventTypes?.Where(t => typeof(IConsumer).IsAssignableFrom(t)).ToArray() ?? [];

        services.AddMassTransit(x =>
        {
            // Descobre automaticamente os Consumers na camada de Application
            x.AddConsumers(applicationAssembly);

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitHost = configuration["RabbitMq:HostName"] ?? "localhost";
                var rabbitUser = configuration["RabbitMq:UserName"] ?? "guest";
                var rabbitPass = configuration["RabbitMq:Password"] ?? "guest";

                cfg.Host(rabbitHost, "/", h =>
                {
                    h.ConnectionName("Fiap.CloudGames.Notifications.Worker");
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ReceiveEndpoint(notificationsCommandsQueue, e =>
                {
                    e.UseConsumeFilter(typeof(TenantIdConsumeFilter<>), context);
                    foreach (var consumerType in consumerCommandTypes)
                    {
                        if (!typeof(IConsumer).IsAssignableFrom(consumerType))
                            throw new InvalidOperationException($"Type {consumerType.FullName} is not a MassTransit consumer.");

                        e.ConfigureConsumer(context, consumerType);
                    }
                });

                cfg.ReceiveEndpoint(notificationsEventsQueue, e =>
                {
                    e.UseConsumeFilter(typeof(TenantIdConsumeFilter<>), context);
                    foreach (var consumerType in consumerEventTypes)
                    {
                        if (!typeof(IConsumer).IsAssignableFrom(consumerType))
                            throw new InvalidOperationException($"Type {consumerType.FullName} is not a MassTransit consumer.");

                        e.ConfigureConsumer(context, consumerType);
                    }
                });
            });
        });

        services.AddHealthChecks();
    }

    /// <summary>
    /// Register infrastructure for SQS-based notification processing (AWS ECS deployment).
    /// Uses AWS SQS polling instead of MassTransit/RabbitMQ.
    /// </summary>
    public static void AddInfrastructureSqs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IEmailService, ConsoleEmailService>();

        var queueUrl = configuration["MAIN_SQS_QUEUE_URL"] 
            ?? throw new InvalidOperationException("MAIN_SQS_QUEUE_URL environment variable not configured.");

        // Register AWS SDK
        services.AddAWSService<IAmazonSQS>();

        // Register the SQS polling background service
        services.AddSingleton<SqsNotificationPollingService>(sp =>
        {
            var sqsClient = sp.GetRequiredService<IAmazonSQS>();
            var emailService = sp.GetRequiredService<IEmailService>();
            var logger = sp.GetRequiredService<ILogger<SqsNotificationPollingService>>();
            return new SqsNotificationPollingService(queueUrl, sqsClient, emailService, logger);
        });

        services.AddHostedService(sp => sp.GetRequiredService<SqsNotificationPollingService>());

        services.AddHealthChecks();
    }
}

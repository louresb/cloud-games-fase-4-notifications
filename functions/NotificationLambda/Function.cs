using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Fiap.CloudGames.Infrastructure.Email.Services;
using Fiap.CloudGames.NotificationLambda.Handlers;
using Fiap.CloudGames.NotificationLambda.Messages;
using Fiap.CloudGames.NotificationLambda.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Text.Json;

// Assembly attribute to enable receiving root-level properties like messageId as input parameter.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Fiap.CloudGames.NotificationLambda;

/// <summary>
/// Minimal AWS Lambda handler for processing SQS notification events.
/// Reuses the notification processing logic from the existing worker consumers.
/// </summary>
public class Function
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Function> _logger;
    private readonly IEventDispatcher _eventDispatcher;

    public Function()
    {
        // Initialize dependency injection
        var services = new ServiceCollection();
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Fiap.CloudGames.Notifications.Lambda")
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });

        // Lambda-specific bootstrap: only register the dependencies needed
        // for SQS message handling (no RabbitMQ/MassTransit/queue settings).
        services.AddSingleton<IEmailService, ConsoleEmailService>();

        // Register event handlers that will be used by the Lambda
        services.AddTransient<PaymentSucceededHandler>();
        services.AddTransient<PaymentFailedHandler>();
        services.AddTransient<PaymentRefundedHandler>();
        services.AddTransient<UserSignedUpHandler>();

        // Register the event dispatcher service
        services.AddSingleton<IEventDispatcher, EventDispatcher>();

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<Function>>();
        _eventDispatcher = _serviceProvider.GetRequiredService<IEventDispatcher>();
    }

    /// <summary>
    /// Lambda handler for processing SQS events from notification queue.
    /// Deserializes SQS message body to domain events and dispatches to appropriate handlers.
    /// </summary>
    /// <param name="sqsEvent">SQS event containing notification messages</param>
    /// <param name="context">Lambda context</param>
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        _logger.LogInformation("Processing SQS batch with {MessageCount} messages", sqsEvent.Records.Count);

        var failedMessageIds = new List<string>();

        foreach (var message in sqsEvent.Records)
        {
            try
            {
                _logger.LogInformation("Processing message with ID: {MessageId}", message.MessageId);
                
                // Parse message body to extract event type and payload
                // Expected format: { "eventType": "UserSignedUp", "payload": {...} }
                var envelope = JsonSerializer.Deserialize<SqsMessageEnvelope>(
                    message.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (envelope == null || string.IsNullOrWhiteSpace(envelope.EventType))
                {
                    _logger.LogWarning("Invalid message envelope format in message {MessageId}", message.MessageId);
                    continue;
                }

                if (envelope.Payload == null)
                {
                    _logger.LogWarning("Missing payload in message {MessageId} for event type {EventType}", 
                        message.MessageId, envelope.EventType);
                    continue;
                }

                _logger.LogInformation("Dispatching event {EventType} from message {MessageId}", 
                    envelope.EventType, message.MessageId);

                // Route to appropriate event handler based on event type
                await _eventDispatcher.DispatchAsync(envelope.EventType, envelope.Payload);

                _logger.LogInformation("Successfully processed message with ID: {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message with ID: {MessageId}", message.MessageId);
                failedMessageIds.Add(message.MessageId);
            }
        }

        if (failedMessageIds.Count > 0)
        {
            _logger.LogWarning("Failed to process {FailedCount} message(s): {FailedIds}", 
                failedMessageIds.Count, 
                string.Join(", ", failedMessageIds));
            
            // AWS Lambda will automatically retry failed messages if they're not deleted from the queue
            // Messages with processing errors will be returned in the SQSBatchResponse
            throw new InvalidOperationException($"Failed to process {failedMessageIds.Count} message(s)");
        }

        _logger.LogInformation("Successfully processed all messages in batch");
    }
}

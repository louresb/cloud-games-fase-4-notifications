using Amazon.SQS;
using Amazon.SQS.Model;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fiap.CloudGames.Infrastructure.Messaging;

/// <summary>
/// Background service that polls AWS SQS queue and dispatches notification events.
/// Minimal event handler mapping for ECS deployment - bypasses MassTransit.
/// </summary>
public class SqsNotificationPollingService : BackgroundService
{
    private readonly string _queueUrl;
    private readonly IAmazonSQS _sqsClient;
    private readonly IEmailService _emailService;
    private readonly ILogger<SqsNotificationPollingService> _logger;
    private const int MaxNumberOfMessages = 10;
    private const int WaitTimeSeconds = 20;

    public SqsNotificationPollingService(
        string queueUrl,
        IAmazonSQS sqsClient,
        IEmailService emailService,
        ILogger<SqsNotificationPollingService> logger)
    {
        _queueUrl = queueUrl;
        _sqsClient = sqsClient;
        _emailService = emailService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS Notification Polling Service started. Queue: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = MaxNumberOfMessages,
                    WaitTimeSeconds = WaitTimeSeconds,
                    AttributeNames = new List<string> { "All" },
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(request, stoppingToken);

                if (response.Messages.Count == 0)
                {
                    continue;
                }

                _logger.LogInformation("Received {MessageCount} messages from SQS", response.Messages.Count);

                var failedMessageHandles = new List<string>();

                foreach (var message in response.Messages)
                {
                    try
                    {
                        await ProcessMessageAsync(message, stoppingToken);
                        
                        // Delete successfully processed message
                        await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                        _logger.LogInformation("Message {MessageId} deleted from queue", message.MessageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process message {MessageId}", message.MessageId);
                        failedMessageHandles.Add(message.ReceiptHandle);
                    }
                }

                // Log summary
                if (failedMessageHandles.Count > 0)
                {
                    _logger.LogWarning("Failed to process {FailedCount} message(s)", failedMessageHandles.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling SQS queue");
                // Back off briefly before retrying
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("SQS Notification Polling Service stopped");
    }

    /// <summary>
    /// Process a single SQS message by deserializing and dispatching by event type.
    /// </summary>
    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing message {MessageId}", message.MessageId);

        var sqsMessage = JsonSerializer.Deserialize<SqsMessage>(
            message.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (sqsMessage == null || string.IsNullOrWhiteSpace(sqsMessage.EventType))
        {
            _logger.LogWarning("Invalid message envelope in message {MessageId}", message.MessageId);
            return;
        }

        if (sqsMessage.Payload == null)
        {
            _logger.LogWarning("Missing payload in message {MessageId} for event type {EventType}",
                message.MessageId, sqsMessage.EventType);
            return;
        }

        _logger.LogInformation("Dispatching event {EventType} from message {MessageId}",
            sqsMessage.EventType, message.MessageId);

        // Dispatch to handler based on event type
        await DispatchEventAsync(sqsMessage.EventType, sqsMessage.Payload);
    }

    /// <summary>
    /// Simple event dispatcher for SQS messages.
    /// Maps event types to email sending logic.
    /// </summary>
    private async Task DispatchEventAsync(string eventType, object payload)
    {
        switch (eventType)
        {
            case "UserSignedUpEvent":
                await HandleUserSignedUpAsync(payload);
                break;
            case "UserEmailConfirmedEvent":
                await HandleUserEmailConfirmedAsync(payload);
                break;
            case "PaymentSucceededEvent":
                await HandlePaymentSucceededAsync(payload);
                break;
            case "PaymentRefundedEvent":
                await HandlePaymentRefundedAsync(payload);
                break;
            case "PaymentFailedEvent":
                await HandlePaymentFailedAsync(payload);
                break;
            default:
                _logger.LogWarning("No handler for event type: {EventType}", eventType);
                break;
        }
    }

    private async Task HandleUserSignedUpAsync(object payload)
    {
        if (payload is not JsonElement jsonElement)
        {
            _logger.LogWarning("UserSignedUpEvent payload is not JsonElement");
            return;
        }

        var name = jsonElement.GetProperty("name").GetString() ?? "User";
        var email = jsonElement.GetProperty("email").GetString();
        var token = jsonElement.GetProperty("confirmationToken").GetString() ?? "";

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("UserSignedUpEvent missing email");
            return;
        }

        var subject = "Bem-vindo ao Cloud Games!";
        var body = $"Olá {name}, obrigado por se cadastrar na nossa plataforma!<br/>" +
                   $"Por favor, confirme seu e-mail usando o seguinte token: {token}";

        await _emailService.SendEmailAsync(email, subject, body);
        _logger.LogInformation("Welcome email sent to {Email}", email);
    }

    private async Task HandleUserEmailConfirmedAsync(object payload)
    {
        if (payload is not JsonElement jsonElement)
        {
            _logger.LogWarning("UserEmailConfirmedEvent payload is not JsonElement");
            return;
        }

        var name = jsonElement.GetProperty("name").GetString() ?? "User";
        var email = jsonElement.GetProperty("email").GetString();

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("UserEmailConfirmedEvent missing email");
            return;
        }

        var subject = "E-mail confirmado";
        var body = $"Olá {name}, seu e-mail foi confirmado com sucesso!";

        await _emailService.SendEmailAsync(email, subject, body);
        _logger.LogInformation("Email confirmed notification sent to {Email}", email);
    }

    private async Task HandlePaymentSucceededAsync(object payload)
    {
        if (payload is not JsonElement jsonElement)
        {
            _logger.LogWarning("PaymentSucceededEvent payload is not JsonElement");
            return;
        }

        var userEmail = jsonElement.GetProperty("userEmail").GetString();
        var orderId = jsonElement.GetProperty("orderId").GetString() ?? "";

        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("PaymentSucceededEvent missing userEmail");
            return;
        }

        var subject = "Pagamento aprovado";
        var body = $"Sua compra (Order: {orderId}) foi aprovada com sucesso!";

        await _emailService.SendEmailAsync(userEmail, subject, body);
        _logger.LogInformation("Payment succeeded notification sent to {Email}", userEmail);
    }

    private async Task HandlePaymentRefundedAsync(object payload)
    {
        if (payload is not JsonElement jsonElement)
        {
            _logger.LogWarning("PaymentRefundedEvent payload is not JsonElement");
            return;
        }

        var userEmail = jsonElement.GetProperty("userEmail").GetString();
        var orderId = jsonElement.GetProperty("orderId").GetString() ?? "";

        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("PaymentRefundedEvent missing userEmail");
            return;
        }

        var subject = "Reembolso processado";
        var body = $"Seu reembolso para a compra (Order: {orderId}) foi processado.";

        await _emailService.SendEmailAsync(userEmail, subject, body);
        _logger.LogInformation("Payment refunded notification sent to {Email}", userEmail);
    }

    private async Task HandlePaymentFailedAsync(object payload)
    {
        if (payload is not JsonElement jsonElement)
        {
            _logger.LogWarning("PaymentFailedEvent payload is not JsonElement");
            return;
        }

        var userEmail = jsonElement.GetProperty("userEmail").GetString();
        var orderId = jsonElement.GetProperty("orderId").GetString() ?? "";

        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("PaymentFailedEvent missing userEmail");
            return;
        }

        var subject = "Pagamento falhou";
        var body = $"Desculpe, seu pagamento para a compra (Order: {orderId}) falhou. Por favor, tente novamente.";

        await _emailService.SendEmailAsync(userEmail, subject, body);
        _logger.LogInformation("Payment failed notification sent to {Email}", userEmail);
    }
}

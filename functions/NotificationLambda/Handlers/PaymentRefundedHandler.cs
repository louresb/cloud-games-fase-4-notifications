using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fiap.CloudGames.NotificationLambda.Handlers;

/// <summary>
/// Handles PaymentRefundedEvent by sending refund confirmation email.
/// Reuses core logic from PaymentRefundedConsumer.
/// </summary>
public class PaymentRefundedHandler(
    ILogger<PaymentRefundedHandler> logger,
    IEmailService emailService) : IEventHandler
{
    private readonly ILogger<PaymentRefundedHandler> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task HandleAsync(object payload)
    {
        try
        {
            // Deserialize payload to domain event
            var jsonElement = (JsonElement)payload;
            var message = JsonSerializer.Deserialize<PaymentRefundedEvent>(
                jsonElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message == null)
                throw new InvalidOperationException("Failed to deserialize PaymentRefundedEvent");

            _logger.LogInformation("Processing: Refund processed for order {OrderId}.", message.OrderId);

            var subject = "Reembolso Processado";
            var body = $"Olá,<br/>" +
                       $"Seu reembolso para o pedido {message.OrderId} foi processado com sucesso em {message.RefundedAt}.";

            await _emailService.SendEmailAsync(message.UserEmail, subject, body);

            _logger.LogInformation("Refund confirmation email sent to {UserEmail}.", message.UserEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PaymentRefundedEvent");
            throw;
        }
    }
}

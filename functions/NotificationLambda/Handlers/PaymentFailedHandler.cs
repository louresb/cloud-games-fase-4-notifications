using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fiap.CloudGames.NotificationLambda.Handlers;

/// <summary>
/// Handles PaymentFailedEvent by sending failure notification email.
/// </summary>
public class PaymentFailedHandler(
    ILogger<PaymentFailedHandler> logger,
    IEmailService emailService) : IEventHandler
{
    private readonly ILogger<PaymentFailedHandler> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task HandleAsync(object payload)
    {
        try
        {
            // Deserialize payload to domain event
            var jsonElement = (JsonElement)payload;
            var message = JsonSerializer.Deserialize<PaymentFailedEvent>(
                jsonElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message == null)
                throw new InvalidOperationException("Failed to deserialize PaymentFailedEvent");

            _logger.LogInformation("Processing: Payment failed for order {OrderId}.", message.OrderId);

            var subject = "Pagamento Não Processado";
            var body = $"Olá,<br/>" +
                       $"Infelizmente, seu pagamento para o pedido {message.OrderId} não pôde ser processado.<br/>" +
                       $"Motivo: {message.FailedReason}<br/>" +
                       $"Por favor, tente novamente.";

            await _emailService.SendEmailAsync(message.UserEmail, subject, body);

            _logger.LogInformation("Payment failure notification sent to {UserEmail}.", message.UserEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PaymentFailedEvent");
            throw;
        }
    }
}

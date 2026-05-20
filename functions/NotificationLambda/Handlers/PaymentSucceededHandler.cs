using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fiap.CloudGames.NotificationLambda.Handlers;

/// <summary>
/// Handles PaymentSucceededEvent by sending confirmation email.
/// Reuses core logic from PaymentSucceededConsumer.
/// </summary>
public class PaymentSucceededHandler(
    ILogger<PaymentSucceededHandler> logger,
    IEmailService emailService) : IEventHandler
{
    private readonly ILogger<PaymentSucceededHandler> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task HandleAsync(object payload)
    {
        try
        {
            // Deserialize payload to domain event
            var jsonElement = (JsonElement)payload;
            var message = JsonSerializer.Deserialize<PaymentSucceededEvent>(
                jsonElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message == null)
                throw new InvalidOperationException("Failed to deserialize PaymentSucceededEvent");

            _logger.LogInformation("Processing: Payment succeeded for order {OrderId}.", message.OrderId);

            var subject = "Pagamento Confirmado!";
            var body = $"Olá,<br/>" +
                       $"Seu pagamento para a transação {message.PaymentTransactionId} foi processado com sucesso em {message.ProcessedAt}.";

            await _emailService.SendEmailAsync(message.UserEmail, subject, body);

            _logger.LogInformation("Payment confirmation email sent to {UserEmail}.", message.UserEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PaymentSucceededEvent");
            throw;
        }
    }
}

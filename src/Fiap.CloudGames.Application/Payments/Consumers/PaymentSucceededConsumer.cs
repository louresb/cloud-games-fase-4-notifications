using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Payments.Consumers;

public class PaymentSucceededConsumer(
    ILogger<PaymentSucceededConsumer> logger,
    IEmailService emailService) : IConsumer<PaymentSucceededEvent>
{
    private readonly ILogger<PaymentSucceededConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Evento recebido: Pagamento bem-sucedido para o pedido {OrderId}.", message.OrderId);

        var subject = "Pagamento Confirmado!";
        var body = $"Olá,<br/>" +
                   $"Seu pagamento para a transação {message.PaymentTransactionId} foi processado com sucesso em {message.ProcessedAt}.";

        await _emailService.SendEmailAsync(message.UserEmail, subject, body);

        _logger.LogInformation("E-mail de confirmação de pagamento enviado para {UserEmail}.", message.UserEmail);
    }
}

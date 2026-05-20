using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Payments.Consumers;

public class PaymentLinkGeneratedConsumer(
    ILogger<PaymentLinkGeneratedConsumer> logger,
    IEmailService emailService) : IConsumer<PaymentLinkGeneratedEvent>
{
    private readonly ILogger<PaymentLinkGeneratedConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task Consume(ConsumeContext<PaymentLinkGeneratedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Evento recebido: Link de pagamento gerado para o pedido {OrderId}.", message.OrderId);

        var subject = "Seu link de pagamento está pronto!";
        var body = $"Olá,<br/>" +
                   $"Seu link de pagamento para a transação {message.PaymentTransactionId} foi gerado com sucesso.<br/>" +
                   $"Por favor, utilize o seguinte link para completar seu pagamento: <a href='{message.PaymentLinkUrl}'>Pagar Agora</a>";

        await _emailService.SendEmailAsync(message.UserEmail, subject, body);

        _logger.LogInformation("E-mail com o link de pagamento enviado para {UserEmail}.", message.UserEmail);
    }
}

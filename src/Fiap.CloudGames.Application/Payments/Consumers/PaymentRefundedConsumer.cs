using Fiap.CloudGames.Application.Payments.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Payments.Consumers;

public class PaymentRefundedConsumer(
    ILogger<PaymentRefundedConsumer> logger,
    IEmailService emailService) : IConsumer<PaymentRefundedEvent>
{
    private readonly ILogger<PaymentRefundedConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task Consume(ConsumeContext<PaymentRefundedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Evento recebido: Reembolso processado para o pedido {OrderId}.", message.OrderId);

        var subject = "Reembolso Processado";
        var body = $"Olá,<br/>" +
                   $"Seu reembolso para o pedido {message.OrderId} foi processado com sucesso em {message.RefundedAt}.";

        await _emailService.SendEmailAsync(message.UserEmail, subject, body);

        _logger.LogInformation("E-mail de confirmação de reembolso enviado para {UserEmail}.", message.UserEmail);
    }
}

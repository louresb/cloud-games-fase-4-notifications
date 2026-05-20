using Fiap.CloudGames.Application.Users.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Users.Consumers;

public class UserPasswordResetedConsumer(
    ILogger<UserPasswordResetedConsumer> logger,
    IEmailService emailService) : IConsumer<UserPasswordResetedEvent>
{
    private readonly ILogger<UserPasswordResetedConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public Task Consume(ConsumeContext<UserPasswordResetedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Evento recebido: Usuário {Name} ({Email}) redefiniu a senha.", message.Name, message.Email);

        var subject = "Senha redefinida com sucesso!";
        var body = $"Olá {message.Name}, sua senha foi redefinida com sucesso. Se você não realizou essa ação, entre em contato conosco imediatamente.";

        return _emailService.SendEmailAsync(message.Email, subject, body);
    }
}

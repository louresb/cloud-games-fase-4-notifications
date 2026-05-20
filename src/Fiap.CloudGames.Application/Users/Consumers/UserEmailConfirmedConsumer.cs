using Fiap.CloudGames.Application.Users.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Users.Consumers;

public class UserEmailConfirmedConsumer(
    ILogger<UserEmailConfirmedConsumer> logger,
    IEmailService emailService) : IConsumer<UserEmailConfirmedEvent>
{
    private readonly ILogger<UserEmailConfirmedConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public Task Consume(ConsumeContext<UserEmailConfirmedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Evento recebido: Usuário {Name} ({Email}) confirmou o e-mail.", message.Name, message.Email);

        var subject = "E-mail confirmado com sucesso!";
        var body = $"Olá {message.Name}, seu e-mail foi confirmado com sucesso. Bem-vindo ao Cloud Games!";

        return _emailService.SendEmailAsync(message.Email, subject, body);
    }
}

using Fiap.CloudGames.Application.Users.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Users.Consumers;

public class UserFirstAccessedConsumer(
    ILogger<UserFirstAccessedConsumer> logger,
    IEmailService emailService) : IConsumer<UserFirstAccessedEvent>
{
    private readonly ILogger<UserFirstAccessedConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public Task Consume(ConsumeContext<UserFirstAccessedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Evento recebido: Usuário {Name} ({Email}) realizou o primeiro acesso.", message.Name, message.Email);

        var subject = "Primeiro acesso realizado!";
        var body = $"Olá {message.Name}, ficamos felizes em ver você acessando o Cloud Games pela primeira vez!";

        return _emailService.SendEmailAsync(message.Email, subject, body);
    }
}

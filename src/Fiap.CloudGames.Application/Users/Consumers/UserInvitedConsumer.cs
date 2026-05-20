using Fiap.CloudGames.Application.Users.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Users.Consumers;

public class UserInvitedConsumer(
    ILogger<UserInvitedConsumer> logger, 
    IEmailService emailService) : IConsumer<UserInvitedEvent>
{
    private readonly ILogger<UserInvitedConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task Consume(ConsumeContext<UserInvitedEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Evento recebido: Usuário {Name} ({Email}) cadastrado via convite de administrador.", message.Name, message.Email);

        var subject = "Bem-vindo ao Cloud Games!";
        var body = $"Olá {message.Name}, você foi convidado para se juntar à nossa plataforma!<br/>" +
                   $"Use o seguinte token para seu primeiro acesso: {message.FirstAccessToken}";

        await _emailService.SendEmailAsync(message.Email, subject, body);
        
        _logger.LogInformation("E-mail de boas-vindas processado.");
    }
}

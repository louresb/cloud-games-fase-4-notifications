using Fiap.CloudGames.Application.Users.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Users.Consumers;

public class UserSignedUpConsumer(IEmailService emailService, ILogger<UserSignedUpConsumer> logger) : IConsumer<UserSignedUpEvent>
{
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<UserSignedUpConsumer> _logger = logger;

    public async Task Consume(ConsumeContext<UserSignedUpEvent> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("Evento recebido: Usuário {Name} ({Email}) se cadastrou.", message.Name, message.Email);

        var subject = "Bem-vindo ao Cloud Games!";
        var body = $"Olá {message.Name}, obrigado por se cadastrar na nossa plataforma!<br/>" +
                   $"Por favor, confirme seu e-mail usando o seguinte token: {message.ConfirmationToken}";

        await _emailService.SendEmailAsync(message.Email, subject, body);
        
        _logger.LogInformation("E-mail de boas-vindas processado.");
    }
}

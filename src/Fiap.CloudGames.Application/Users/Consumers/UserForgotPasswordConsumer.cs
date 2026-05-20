using Fiap.CloudGames.Application.Users.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Application.Users.Consumers;

public class UserForgotPasswordConsumer(
    ILogger<UserForgotPasswordConsumer> logger,
    IEmailService emailService) : IConsumer<UserForgotPasswordEvent>
{
    private readonly ILogger<UserForgotPasswordConsumer> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public Task Consume(ConsumeContext<UserForgotPasswordEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("Evento recebido: Usuário {Name} ({Email}) solicitou recuperação de senha.", message.Name, message.Email);

        var subject = "Recuperação de senha";
        var body = $"Olá {message.Name}, você solicitou a recuperação de senha. Use o seguinte token para redefinir sua senha: {message.ResetToken}";

        return _emailService.SendEmailAsync(message.Email, subject, body);
    }
}

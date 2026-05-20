using Fiap.CloudGames.Application.Users.Events;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Fiap.CloudGames.NotificationLambda.Handlers;

/// <summary>
/// Handles UserSignedUpEvent by sending welcome email.
/// Reuses core logic from UserSignedUpConsumer.
/// </summary>
public class UserSignedUpHandler(
    ILogger<UserSignedUpHandler> logger,
    IEmailService emailService) : IEventHandler
{
    private readonly ILogger<UserSignedUpHandler> _logger = logger;
    private readonly IEmailService _emailService = emailService;

    public async Task HandleAsync(object payload)
    {
        try
        {
            // Deserialize payload to domain event
            var jsonElement = (JsonElement)payload;
            var message = JsonSerializer.Deserialize<UserSignedUpEvent>(
                jsonElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (message == null)
                throw new InvalidOperationException("Failed to deserialize UserSignedUpEvent");

            _logger.LogInformation("Processing: User {Name} ({Email}) signed up.", message.Name, message.Email);

            var subject = "Bem-vindo ao Cloud Games!";
            var body = $"Olá {message.Name}, obrigado por se cadastrar na nossa plataforma!<br/>" +
                       $"Por favor, confirme seu e-mail usando o seguinte token: {message.ConfirmationToken}";

            await _emailService.SendEmailAsync(message.Email, subject, body);

            _logger.LogInformation("Welcome email sent to {UserEmail}.", message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserSignedUpEvent");
            throw;
        }
    }
}

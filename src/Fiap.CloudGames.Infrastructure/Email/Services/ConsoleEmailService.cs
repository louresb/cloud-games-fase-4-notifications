using System;
using Fiap.CloudGames.Domain.Email.Interfaces;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.Infrastructure.Email.Services;

public class ConsoleEmailService(ILogger<ConsoleEmailService> logger) : IEmailService
{
	private readonly ILogger<ConsoleEmailService> _logger = logger;

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogWarning("---------------------------------------------------");
        _logger.LogWarning("SIMULAÇÃO DE ENVIO DE E-MAIL");
        _logger.LogWarning("DESTINATÁRIO: {to}", to);
        _logger.LogWarning("ASSUNTO: {subject}", subject);
        _logger.LogWarning("CORPO: {body}", body);
        _logger.LogWarning("---------------------------------------------------");
        
        return Task.CompletedTask;
    }
}

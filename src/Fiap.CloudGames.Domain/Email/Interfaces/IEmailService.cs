namespace Fiap.CloudGames.Domain.Email.Interfaces;

public interface IEmailService
{
	Task SendEmailAsync(string email, string subject, string body, CancellationToken ct = default);
}

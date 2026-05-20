namespace Fiap.CloudGames.Application.Users.Events;

public record UserSignedUpEvent(Guid Id, string Name, string Email, string ConfirmationToken);
public record UserEmailConfirmedEvent(Guid Id, string Name, string Email);
public record UserInvitedEvent(Guid Id, string Name, string Email, string FirstAccessToken);
public record UserFirstAccessedEvent(Guid Id, string Name, string Email);
public record UserForgotPasswordEvent(Guid Id, string Name, string Email, string ResetToken);
public record UserPasswordResetedEvent(Guid Id, string Name, string Email);

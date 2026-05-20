namespace Fiap.CloudGames.NotificationLambda.Handlers;

/// <summary>
/// Base handler interface for processing domain events.
/// </summary>
public interface IEventHandler
{
    /// <summary>
    /// Process the event payload.
    /// </summary>
    /// <param name="payload">Event payload object</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task HandleAsync(object payload);
}

using Fiap.CloudGames.NotificationLambda.Handlers;
using Microsoft.Extensions.Logging;

namespace Fiap.CloudGames.NotificationLambda.Services;

/// <summary>
/// Simple event dispatcher that routes events to appropriate handlers.
/// Maps event type names to handler implementations.
/// </summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Dispatch an event to the appropriate handler based on event type.
    /// </summary>
    /// <param name="eventType">Name of the event type (e.g., "UserSignedUp")</param>
    /// <param name="payload">Event payload object</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DispatchAsync(string eventType, object payload);
}

/// <summary>
/// Default implementation of event dispatcher.
/// Uses dependency injection to resolve handlers for each event type.
/// </summary>
public class EventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<EventDispatcher> logger) : IEventDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<EventDispatcher> _logger = logger;

    // Map event type names to handler types
    private static readonly Dictionary<string, Type> EventHandlerMap = new()
    {
        { "PaymentSucceededEvent", typeof(PaymentSucceededHandler) },
        { "PaymentFailedEvent", typeof(PaymentFailedHandler) },
        { "PaymentRefundedEvent", typeof(PaymentRefundedHandler) },
        { "UserSignedUpEvent", typeof(UserSignedUpHandler) },
    };

    public async Task DispatchAsync(string eventType, object payload)
    {
        if (!EventHandlerMap.TryGetValue(eventType, out var handlerType))
        {
            _logger.LogWarning("No handler registered for event type: {EventType}", eventType);
            return;
        }

        try
        {
            var handler = _serviceProvider.GetService(handlerType) as IEventHandler;
            if (handler == null)
            {
                _logger.LogError("Failed to resolve handler for event type: {EventType}", eventType);
                throw new InvalidOperationException($"Handler not registered for {eventType}");
            }

            _logger.LogInformation("Dispatching event {EventType} to handler {HandlerType}", eventType, handlerType.Name);
            await handler.HandleAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching event {EventType}", eventType);
            throw;
        }
    }
}

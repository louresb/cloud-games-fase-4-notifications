namespace Fiap.CloudGames.NotificationLambda.Messages;

/// <summary>
/// Simple envelope for SQS message payloads.
/// SQS message body should contain JSON in this format:
/// { "eventType": "UserSignedUp", "payload": {...} }
/// </summary>
public record SqsMessageEnvelope
{
    public string EventType { get; init; } = string.Empty;
    public object? Payload { get; init; }
}

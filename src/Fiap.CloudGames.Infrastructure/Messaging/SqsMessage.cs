namespace Fiap.CloudGames.Infrastructure.Messaging;

/// <summary>
/// Envelope for SQS message payloads.
/// SQS message body should contain JSON: { "eventType": "UserSignedUp", "payload": {...} }
/// </summary>
public class SqsMessage
{
    public string EventType { get; set; } = string.Empty;
    public object? Payload { get; set; }
}

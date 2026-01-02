namespace Offers.Infrastructure.Services;

/// <summary>
/// Interface for publishing messages to a message broker.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default);
}

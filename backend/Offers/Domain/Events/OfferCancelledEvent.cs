namespace Offers.Domain.Events;

/// <summary>
/// Immutable domain event representing the cancellation of an offer.
/// </summary>
public record OfferCancelledEvent
{
    public Guid OfferId { get; init; }
    public Guid SellerId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = "Cancelled";
    public DateTime CancelledAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "OfferCancelled";
}

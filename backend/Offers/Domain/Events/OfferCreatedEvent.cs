using Offers.Domain.ValueObjects;

namespace Offers.Domain.Events;

/// <summary>
/// Immutable domain event representing the creation of a new offer.
/// Contains a fully denormalized snapshot of the offer data for downstream consumers.
/// </summary>
public record OfferCreatedEvent
{
    /// <summary>
    /// Unique identifier of the offer.
    /// </summary>
    public int OfferId { get; init; }

    /// <summary>
    /// Identifier of the seller who created the offer.
    /// </summary>
    public int SellerId { get; init; }

    /// <summary>
    /// Vehicle Identification Number.
    /// </summary>
    public string Vin { get; init; } = string.Empty;

    /// <summary>
    /// Vehicle manufacturer.
    /// </summary>
    public string Make { get; init; } = string.Empty;

    /// <summary>
    /// Vehicle model name.
    /// </summary>
    public string Model { get; init; } = string.Empty;

    /// <summary>
    /// Vehicle model year.
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Offer price in USD.
    /// </summary>
    public decimal OfferAmount { get; init; }

    /// <summary>
    /// Vehicle location details.
    /// </summary>
    public Location Location { get; init; } = new();

    /// <summary>
    /// Vehicle condition details.
    /// </summary>
    public Condition Condition { get; init; } = new();

    /// <summary>
    /// Current status of the offer.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the offer was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when this event was generated.
    /// </summary>
    public DateTime EventTimestamp { get; init; }

    /// <summary>
    /// Event type identifier for routing.
    /// </summary>
    public string EventType => "OfferCreated";
}

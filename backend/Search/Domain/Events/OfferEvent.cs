namespace Search.Domain.Events;

/// <summary>
/// Event received when an offer is created or updated.
/// </summary>
public record OfferEvent
{
    public string EventType { get; init; } = string.Empty;
    public Guid OfferId { get; init; }
    public Guid SellerId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal OfferAmount { get; init; }
    public OfferLocation? Location { get; init; }
    public OfferCondition? Condition { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
}

public record OfferLocation
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string StateCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
}

public record OfferCondition
{
    public string Grade { get; init; } = string.Empty;
    public int? Mileage { get; init; }
    public bool HasAccidentHistory { get; init; }
    public string Description { get; init; } = string.Empty;
}

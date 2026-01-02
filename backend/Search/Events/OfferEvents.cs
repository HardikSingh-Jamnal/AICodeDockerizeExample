namespace Search.Events;

/// <summary>
/// Event received when an offer is created.
/// </summary>
public record OfferCreatedEvent
{
    public Guid OfferId { get; init; }
    public Guid SellerId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal OfferAmount { get; init; }
    public LocationDto Location { get; init; } = new();
    public ConditionDto Condition { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "OfferCreated";
}

/// <summary>
/// Event received when an offer is updated.
/// </summary>
public record OfferUpdatedEvent
{
    public Guid OfferId { get; init; }
    public Guid SellerId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal OfferAmount { get; init; }
    public LocationDto Location { get; init; } = new();
    public ConditionDto Condition { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "OfferUpdated";
}

/// <summary>
/// Event received when an offer is cancelled.
/// </summary>
public record OfferCancelledEvent
{
    public Guid OfferId { get; init; }
    public Guid SellerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? UpdatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "OfferCancelled";
}

/// <summary>
/// Location value object DTO.
/// </summary>
public record LocationDto
{
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = "USA";
}

/// <summary>
/// Condition value object DTO.
/// </summary>
public record ConditionDto
{
    public int Mileage { get; init; }
    public string Exterior { get; init; } = string.Empty;
    public string Interior { get; init; } = string.Empty;
    public string Mechanical { get; init; } = string.Empty;
}

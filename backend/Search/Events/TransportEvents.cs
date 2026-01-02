namespace Search.Events;

/// <summary>
/// Event received when a transport is created.
/// </summary>
public record TransportCreatedEvent
{
    public int TransportId { get; init; }
    public int CarrierId { get; init; }
    public int PurchaseId { get; init; }
    public string PickupStreet { get; init; } = string.Empty;
    public string PickupCity { get; init; } = string.Empty;
    public string PickupStateCode { get; init; } = string.Empty;
    public string PickupCountry { get; init; } = string.Empty;
    public string PickupZipCode { get; init; } = string.Empty;
    public string DeliveryStreet { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryStateCode { get; init; } = string.Empty;
    public string DeliveryCountry { get; init; } = string.Empty;
    public string DeliveryZipCode { get; init; } = string.Empty;
    public DateTime ScheduleDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal? EstimatedCost { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "TransportCreated";
}

/// <summary>
/// Event received when a transport is updated.
/// </summary>
public record TransportUpdatedEvent
{
    public int TransportId { get; init; }
    public int CarrierId { get; init; }
    public int PurchaseId { get; init; }
    public string PickupStreet { get; init; } = string.Empty;
    public string PickupCity { get; init; } = string.Empty;
    public string PickupStateCode { get; init; } = string.Empty;
    public string PickupCountry { get; init; } = string.Empty;
    public string PickupZipCode { get; init; } = string.Empty;
    public string DeliveryStreet { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryStateCode { get; init; } = string.Empty;
    public string DeliveryCountry { get; init; } = string.Empty;
    public string DeliveryZipCode { get; init; } = string.Empty;
    public DateTime ScheduleDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal? EstimatedCost { get; init; }
    public decimal? ActualCost { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "TransportUpdated";
}

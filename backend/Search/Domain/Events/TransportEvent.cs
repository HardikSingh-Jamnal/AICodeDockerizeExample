namespace Search.Domain.Events;

/// <summary>
/// Event received when a transport is created or updated.
/// </summary>
public record TransportEvent
{
    public string EventType { get; init; } = string.Empty;
    public int TransportId { get; init; }
    public int CarrierId { get; init; }
    public int PurchaseId { get; init; }
    public string PickupCity { get; init; } = string.Empty;
    public string PickupStateCode { get; init; } = string.Empty;
    public string PickupCountry { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryStateCode { get; init; } = string.Empty;
    public string DeliveryCountry { get; init; } = string.Empty;
    public DateTime ScheduleDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal? EstimatedCost { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
}

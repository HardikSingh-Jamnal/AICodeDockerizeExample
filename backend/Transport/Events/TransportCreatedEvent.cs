namespace Transport.Events;

/// <summary>
/// Domain event representing the creation of a new transport.
/// </summary>
public record TransportCreatedEvent
{
    public int TransportId { get; init; }
    public int CarrierId { get; init; }
    public int PurchaseId { get; init; }
    public string PickupCity { get; init; } = string.Empty;
    public string PickupStateCode { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryStateCode { get; init; } = string.Empty;
    public DateTime ScheduleDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal? EstimatedCost { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "TransportCreated";
}

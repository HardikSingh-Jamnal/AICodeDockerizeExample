namespace Search.Events;

/// <summary>
/// Event received when a purchase is created.
/// </summary>
public record PurchaseCreatedEvent
{
    public int PurchaseId { get; init; }
    public int BuyerId { get; init; }
    public int OfferId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "PurchaseCreated";
}

/// <summary>
/// Event received when a purchase is updated.
/// </summary>
public record PurchaseUpdatedEvent
{
    public int PurchaseId { get; init; }
    public int BuyerId { get; init; }
    public int OfferId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
    public string EventType => "PurchaseUpdated";
}

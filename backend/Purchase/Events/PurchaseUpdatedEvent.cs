namespace Purchase.Events;

/// <summary>
/// Domain event representing an update to an existing purchase.
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

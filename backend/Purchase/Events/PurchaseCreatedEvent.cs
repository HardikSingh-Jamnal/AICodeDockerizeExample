namespace Purchase.Events;

/// <summary>
/// Domain event representing the creation of a new purchase.
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

namespace Search.Domain.Events;

/// <summary>
/// Event received when a purchase is created or updated.
/// </summary>
public record PurchaseEvent
{
    public string EventType { get; init; } = string.Empty;
    public int PurchaseId { get; init; }
    public int BuyerId { get; init; }
    public int OfferId { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime EventTimestamp { get; init; }
}

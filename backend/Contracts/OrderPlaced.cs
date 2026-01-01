namespace Contracts;

public record OrderPlaced
{
    public Guid OrderId { get; init; }
    public Guid[] ProductIds { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime OrderDate { get; init; }
}

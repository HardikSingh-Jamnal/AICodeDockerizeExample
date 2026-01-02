namespace Search.Documents;

/// <summary>
/// Elasticsearch document representing a purchase.
/// </summary>
public class PurchaseDocument : BaseDocument
{
    public override string EntityType => "Purchase";

    public int PurchaseId { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Builds the searchable text from all relevant fields.
    /// </summary>
    public void BuildSearchableText()
    {
        SearchableText = $"Purchase {PurchaseId} Buyer {BuyerId} Offer {OfferId} {Status}";
    }
}

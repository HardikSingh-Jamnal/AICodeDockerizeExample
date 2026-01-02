namespace Search.Documents;

/// <summary>
/// Elasticsearch document representing a transport.
/// </summary>
public class TransportDocument : BaseDocument
{
    public override string EntityType { get; set; } = "Transport";

    public int TransportId { get; set; }
    public int CarrierId { get; set; }
    public int PurchaseId { get; set; }

    // Pickup location
    public string PickupStreet { get; set; } = string.Empty;
    public string PickupCity { get; set; } = string.Empty;
    public string PickupState { get; set; } = string.Empty;
    public string PickupZipCode { get; set; } = string.Empty;
    public string PickupCountry { get; set; } = string.Empty;

    // Delivery location
    public string DeliveryStreet { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public string DeliveryZipCode { get; set; } = string.Empty;
    public string DeliveryCountry { get; set; } = string.Empty;

    public DateTime ScheduleDate { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Builds the searchable text from all relevant fields.
    /// </summary>
    public void BuildSearchableText()
    {
        SearchableText = $"Transport {TransportId} Carrier {CarrierId} {PickupCity} {PickupState} to {DeliveryCity} {DeliveryState} {Status} {Notes}";
    }
}

namespace Search.Documents;

/// <summary>
/// Elasticsearch document representing a vehicle offer.
/// </summary>
public class OfferDocument : BaseDocument
{
    public override string EntityType { get; set; } = "Offer";

    public Guid OfferId { get; set; }
    public Guid SellerId { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal OfferAmount { get; set; }

    // Location details (flattened from nested object)
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    // Condition details (flattened from nested object)
    public int Mileage { get; set; }
    public string ExteriorCondition { get; set; } = string.Empty;
    public string InteriorCondition { get; set; } = string.Empty;
    public string MechanicalCondition { get; set; } = string.Empty;

    /// <summary>
    /// Builds the searchable text from all relevant fields.
    /// </summary>
    public void BuildSearchableText()
    {
        SearchableText = $"{Vin} {Make} {Model} {Year} {City} {State} {ZipCode} {Status} {ExteriorCondition} {InteriorCondition}";
    }
}

using Nest;
using Search.Domain.Enums;

namespace Search.Domain.Entities;

/// <summary>
/// Unified search document that represents any searchable entity in the marketplace.
/// This schema is indexed in Elasticsearch for cross-entity search.
/// </summary>
[ElasticsearchType(IdProperty = nameof(Id))]
public class SearchDocument
{
    /// <summary>
    /// Composite ID: EntityType_EntityId (e.g., "Offer_abc123")
    /// </summary>
    [Keyword]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity (Offer, Purchase, Transport)
    /// </summary>
    [Keyword]
    public EntityType EntityType { get; set; }

    /// <summary>
    /// Original entity ID from the source service
    /// </summary>
    [Keyword]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Searchable title - derived from entity data
    /// For offers: "Make Model Year"
    /// For purchases: "Purchase of Make Model"
    /// For transports: "Transport to City"
    /// </summary>
    [Text(Analyzer = "standard")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Full-text searchable description containing all relevant details
    /// </summary>
    [Text(Analyzer = "standard")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Keywords for exact matching (VIN, status codes, etc.)
    /// </summary>
    [Keyword]
    public List<string> Keywords { get; set; } = new();

    // Entity ownership fields for access control
    
    [Keyword]
    public string? SellerId { get; set; }

    [Keyword]
    public string? BuyerId { get; set; }

    [Keyword]
    public string? CarrierId { get; set; }

    // Vehicle information (from Offers)
    
    [Keyword]
    public string? Vin { get; set; }

    [Text]
    public string? Make { get; set; }

    [Text]
    public string? Model { get; set; }

    [Number(NumberType.Integer)]
    public int? Year { get; set; }

    // Financial information
    
    [Number(NumberType.Double)]
    public decimal? Amount { get; set; }

    // Status and location
    
    [Keyword]
    public string? Status { get; set; }

    [Text]
    public string? Location { get; set; }

    [Keyword]
    public string? City { get; set; }

    [Keyword]
    public string? State { get; set; }

    [Keyword]
    public string? Country { get; set; }

    // Audit timestamps
    
    [Date]
    public DateTime CreatedAt { get; set; }

    [Date]
    public DateTime? UpdatedAt { get; set; }

    [Date]
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}

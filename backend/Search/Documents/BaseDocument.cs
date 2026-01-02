using Nest;

namespace Search.Documents;

/// <summary>
/// Base document with common properties for all searchable entities.
/// </summary>
public class BaseDocument
{
    /// <summary>
    /// Unique document ID in format "{entityType}_{entityId}"
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity (Offer, Purchase, Transport)
    /// </summary>
    public virtual string EntityType { get; set; } = "Base";

    /// <summary>
    /// Combined searchable text for full-text search
    /// </summary>
    public string SearchableText { get; set; } = string.Empty;

    /// <summary>
    /// Status of the entity
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

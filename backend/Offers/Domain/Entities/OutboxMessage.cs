using System.ComponentModel.DataAnnotations;

namespace Offers.Domain.Entities;

/// <summary>
/// Represents a message in the transactional outbox pattern.
/// Messages are written to this table within the same transaction as the domain event,
/// ensuring at-least-once delivery.
/// </summary>
public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Type of the event (e.g., "OfferCreated", "OfferUpdated").
    /// Used for routing to appropriate handlers.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized event payload.
    /// </summary>
    [Required]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the message was successfully processed (published).
    /// Null if not yet processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of times processing has been attempted.
    /// Used for retry logic with exponential backoff.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Error message from the last failed processing attempt.
    /// </summary>
    public string? LastError { get; set; }
}

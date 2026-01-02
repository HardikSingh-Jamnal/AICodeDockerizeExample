using System.ComponentModel.DataAnnotations;

namespace Transport.Entities;

/// <summary>
/// Represents a message in the transactional outbox pattern.
/// </summary>
public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; } = 0;

    public string? LastError { get; set; }
}

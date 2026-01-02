using System.ComponentModel.DataAnnotations;
using Offers.Domain.Enums;
using Offers.Domain.ValueObjects;

namespace Offers.Domain.Entities;

/// <summary>
/// Represents a vehicle offer created by a seller.
/// This is the aggregate root for the Offers bounded context.
/// </summary>
public class Offer
{
    [Key]
    public int OfferId { get; set; }

    [Required]
    public int SellerId { get; set; }

    /// <summary>
    /// Vehicle Identification Number - must be exactly 17 characters.
    /// Unique per seller.
    /// </summary>
    [Required]
    [StringLength(17, MinimumLength = 17)]
    public string Vin { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Model year (valid range: 1900 to current year + 1).
    /// </summary>
    [Required]
    [Range(1900, 2100)]
    public int Year { get; set; }

    /// <summary>
    /// Offer amount in USD. Must be positive.
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal OfferAmount { get; set; }

    /// <summary>
    /// Vehicle location stored as JSONB.
    /// </summary>
    [Required]
    public Location Location { get; set; } = new();

    /// <summary>
    /// Vehicle condition stored as JSONB.
    /// </summary>
    [Required]
    public Condition Condition { get; set; } = new();

    /// <summary>
    /// Current status of the offer.
    /// </summary>
    [Required]
    public OfferStatus Status { get; set; } = OfferStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Checks if the offer can be updated based on its current status.
    /// </summary>
    public bool CanBeUpdated() => Status is OfferStatus.Active or OfferStatus.Pending;

    /// <summary>
    /// Checks if the offer can be cancelled based on its current status.
    /// </summary>
    public bool CanBeCancelled() => Status is OfferStatus.Active or OfferStatus.Pending;

    /// <summary>
    /// Cancels the offer if allowed by business rules.
    /// </summary>
    /// <returns>True if cancellation was successful, false otherwise.</returns>
    public bool Cancel()
    {
        if (!CanBeCancelled())
            return false;

        Status = OfferStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Updates the offer details if allowed by business rules.
    /// </summary>
    public bool Update(decimal? offerAmount = null, Location? location = null, Condition? condition = null)
    {
        if (!CanBeUpdated())
            return false;

        if (offerAmount.HasValue && offerAmount.Value > 0)
            OfferAmount = offerAmount.Value;

        if (location != null)
            Location = location;

        if (condition != null)
            Condition = condition;

        UpdatedAt = DateTime.UtcNow;
        return true;
    }
}

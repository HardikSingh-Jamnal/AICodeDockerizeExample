namespace Offers.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of an offer.
/// </summary>
public enum OfferStatus
{
    /// <summary>
    /// Offer is active and available.
    /// </summary>
    Active,

    /// <summary>
    /// Offer is pending review or approval.
    /// </summary>
    Pending,

    /// <summary>
    /// Vehicle has been sold.
    /// </summary>
    Sold,

    /// <summary>
    /// Offer has been cancelled by the seller.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Offer has expired.
    /// </summary>
    Expired
}

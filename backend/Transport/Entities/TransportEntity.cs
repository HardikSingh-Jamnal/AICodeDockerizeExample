using System.ComponentModel.DataAnnotations;

namespace Transport.Entities;

public class TransportEntity
{
    public int TransportId { get; set; }
    
    [Required]
    public int CarrierId { get; set; }
    
    [Required]
    public int PurchaseId { get; set; }
    
    // Pickup Address Fields
    [Required]
    [StringLength(200)]
    public string PickupStreet { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string PickupCity { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10)]
    public string PickupStateCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string PickupCountry { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string PickupZipCode { get; set; } = string.Empty;
    
    // Delivery Address Fields
    [Required]
    [StringLength(200)]
    public string DeliveryStreet { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string DeliveryCity { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10)]
    public string DeliveryStateCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string DeliveryCountry { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string DeliveryZipCode { get; set; } = string.Empty;
    
    [Required]
    public DateTime ScheduleDate { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, InTransit, Delivered, Cancelled
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
    
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
}
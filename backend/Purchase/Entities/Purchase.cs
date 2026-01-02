using System.ComponentModel.DataAnnotations;

namespace Purchase.Entities;

public class Purchase
{
    public int Id { get; set; }

    [Required]
    public int BuyerId { get; set; }

    [Required]
    public int OfferId { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

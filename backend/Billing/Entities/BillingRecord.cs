using System.ComponentModel.DataAnnotations;

namespace Billing.Entities;

public class BillingRecord
{
    public int Id { get; set; }
    
    [Required]
    public int OrderId { get; set; }
    
    [Required]
    public int CustomerId { get; set; }
    
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string CustomerEmail { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TaxAmount { get; set; }
    
    public decimal TotalAmount => Amount + TaxAmount;
    
    public BillingStatus Status { get; set; } = BillingStatus.Pending;
    
    [StringLength(200)]
    public string BillingAddress { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string TransactionId { get; set; } = string.Empty;
    
    public DateTime BillingDate { get; set; } = DateTime.UtcNow;
    public DateTime? PaidDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public enum BillingStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3,
    Cancelled = 4
}
using System.ComponentModel.DataAnnotations;

namespace Order.Entities;

public class OrderEntity
{
    public int Id { get; set; }
    
    [Required]
    public int CustomerId { get; set; }
    
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string CustomerEmail { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }
    
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    [StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;
    
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    
    public List<OrderItem> OrderItems { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public OrderEntity Order { get; set; } = null!;
    
    public int ProductId { get; set; }
    
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    
    public decimal TotalPrice => UnitPrice * Quantity;
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
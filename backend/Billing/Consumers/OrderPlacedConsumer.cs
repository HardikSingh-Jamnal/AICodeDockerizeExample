using Billing.Data;
using Billing.Entities;
using Contracts;
using MassTransit;

namespace Billing.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly BillingDbContext _context;

    public OrderPlacedConsumer(BillingDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        try
        {
            var order = context.Message;

            // Calculate tax (10% of order amount)
            var taxAmount = Math.Round(order.TotalAmount * 0.1m, 2);
            var totalWithTax = order.TotalAmount + taxAmount;

            var billingRecord = new BillingRecord
            {
                OrderId = Convert.ToInt32(order.OrderId.ToString().Substring(0, 8), 16), // Use first 8 chars of GUID as int
                Amount = order.TotalAmount,
                TaxAmount = taxAmount,
                BillingAddress = "Default Billing Address", // In real scenario, this would come from order data
                CustomerEmail = "customer@example.com", // In real scenario, this would come from order data
                CustomerId = 1, // In real scenario, this would come from order data
                CustomerName = "Customer Name", // In real scenario, this would come from order data
                BillingDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                PaymentMethod = "Pending",
                Status = 0 // Pending status
            };

            _context.BillingRecords.Add(billingRecord);
            await _context.SaveChangesAsync();
            
            // Log successful processing
            Console.WriteLine($"Created billing record for order {order.OrderId} with amount ${totalWithTax}");
        }
        catch (Exception ex)
        {
            // Log error and rethrow for MassTransit retry handling
            Console.WriteLine($"Error processing OrderPlaced event: {ex.Message}");
            throw;
        }
    }
}


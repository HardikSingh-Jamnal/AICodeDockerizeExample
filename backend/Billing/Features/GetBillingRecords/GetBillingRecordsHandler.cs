using MediatR;
using Microsoft.EntityFrameworkCore;
using Billing.Data;
using Billing.Entities;

namespace Billing.Features.GetBillingRecords;

public record GetBillingRecordsQuery : IRequest<IEnumerable<BillingRecordDto>>;

public record BillingRecordDto(int Id, int OrderId, int CustomerId, string CustomerName, string CustomerEmail, decimal Amount, decimal TaxAmount, decimal TotalAmount, BillingStatus Status, string BillingAddress, string PaymentMethod, string TransactionId, DateTime BillingDate, DateTime? PaidDate, DateTime? DueDate);

public class GetBillingRecordsHandler : IRequestHandler<GetBillingRecordsQuery, IEnumerable<BillingRecordDto>>
{
    private readonly BillingDbContext _context;

    public GetBillingRecordsHandler(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BillingRecordDto>> Handle(GetBillingRecordsQuery request, CancellationToken cancellationToken)
    {
        return await _context.BillingRecords
            .Select(b => new BillingRecordDto(b.Id, b.OrderId, b.CustomerId, b.CustomerName, b.CustomerEmail, b.Amount, b.TaxAmount, b.TotalAmount, b.Status, b.BillingAddress, b.PaymentMethod, b.TransactionId, b.BillingDate, b.PaidDate, b.DueDate))
            .ToListAsync(cancellationToken);
    }
}
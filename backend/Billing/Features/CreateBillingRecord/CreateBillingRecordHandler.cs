using FluentValidation;
using MediatR;
using Billing.Data;
using Billing.Entities;

namespace Billing.Features.CreateBillingRecord;

public record CreateBillingRecordCommand(int OrderId, int CustomerId, string CustomerName, string CustomerEmail, decimal Amount, decimal TaxAmount, string BillingAddress, string PaymentMethod) : IRequest<int>;

public class CreateBillingRecordValidator : AbstractValidator<CreateBillingRecordCommand>
{
    public CreateBillingRecordValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BillingAddress).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(100);
    }
}

public class CreateBillingRecordHandler : IRequestHandler<CreateBillingRecordCommand, int>
{
    private readonly BillingDbContext _context;

    public CreateBillingRecordHandler(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateBillingRecordCommand request, CancellationToken cancellationToken)
    {
        var billingRecord = new BillingRecord
        {
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Amount = request.Amount,
            TaxAmount = request.TaxAmount,
            BillingAddress = request.BillingAddress,
            PaymentMethod = request.PaymentMethod,
            DueDate = DateTime.UtcNow.AddDays(30) // 30 days payment term
        };

        _context.BillingRecords.Add(billingRecord);
        await _context.SaveChangesAsync(cancellationToken);

        return billingRecord.Id;
    }
}
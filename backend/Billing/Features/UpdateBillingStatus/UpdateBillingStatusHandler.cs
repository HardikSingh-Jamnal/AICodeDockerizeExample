using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Billing.Data;
using Billing.Entities;

namespace Billing.Features.UpdateBillingStatus;

public record UpdateBillingStatusCommand(int Id, BillingStatus Status, string? TransactionId = null) : IRequest<bool>;

public class UpdateBillingStatusValidator : AbstractValidator<UpdateBillingStatusCommand>
{
    public UpdateBillingStatusValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateBillingStatusHandler : IRequestHandler<UpdateBillingStatusCommand, bool>
{
    private readonly BillingDbContext _context;

    public UpdateBillingStatusHandler(BillingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateBillingStatusCommand request, CancellationToken cancellationToken)
    {
        var billingRecord = await _context.BillingRecords
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (billingRecord == null) return false;

        billingRecord.Status = request.Status;
        
        if (!string.IsNullOrEmpty(request.TransactionId))
            billingRecord.TransactionId = request.TransactionId;
            
        if (request.Status == BillingStatus.Paid && billingRecord.PaidDate == null)
            billingRecord.PaidDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
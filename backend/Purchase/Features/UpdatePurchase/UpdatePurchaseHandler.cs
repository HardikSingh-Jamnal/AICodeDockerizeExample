using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Purchase.Data;

namespace Purchase.Features.UpdatePurchase;

public record UpdatePurchaseCommand(int BuyerId, int OfferId, decimal Amount, string Status) : IRequest<bool>
{
    public int Id { get; init; }
}

public class UpdatePurchaseValidator : AbstractValidator<UpdatePurchaseCommand>
{
    public UpdatePurchaseValidator()
    {
        RuleFor(x => x.BuyerId).GreaterThan(0);
        RuleFor(x => x.OfferId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
    }
}

public class UpdatePurchaseHandler : IRequestHandler<UpdatePurchaseCommand, bool>
{
    private readonly PurchasesDbContext _context;

    public UpdatePurchaseHandler(PurchasesDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdatePurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _context.Purchases
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

        if (purchase == null) return false;

        purchase.BuyerId = request.BuyerId;
        purchase.OfferId = request.OfferId;
        purchase.Amount = request.Amount;
        purchase.Status = request.Status;
        purchase.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

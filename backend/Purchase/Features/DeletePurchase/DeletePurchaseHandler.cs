using MediatR;
using Microsoft.EntityFrameworkCore;
using Purchase.Data;

namespace Purchase.Features.DeletePurchase;

public record DeletePurchaseCommand(int Id) : IRequest<bool>;

public class DeletePurchaseHandler : IRequestHandler<DeletePurchaseCommand, bool>
{
    private readonly PurchasesDbContext _context;

    public DeletePurchaseHandler(PurchasesDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeletePurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = await _context.Purchases
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

        if (purchase == null) return false;

        purchase.IsActive = false;
        purchase.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

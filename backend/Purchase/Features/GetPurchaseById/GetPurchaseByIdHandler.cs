using MediatR;
using Microsoft.EntityFrameworkCore;
using Purchase.Data;

namespace Purchase.Features.GetPurchaseById;

public record GetPurchaseByIdQuery(int Id) : IRequest<PurchaseDto?>;

public record PurchaseDto(int Id, int BuyerId, int OfferId, DateTime PurchaseDate, decimal Amount, string Status, bool IsActive);

public class GetPurchaseByIdHandler : IRequestHandler<GetPurchaseByIdQuery, PurchaseDto?>
{
    private readonly PurchasesDbContext _context;

    public GetPurchaseByIdHandler(PurchasesDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseDto?> Handle(GetPurchaseByIdQuery request, CancellationToken cancellationToken)
    {
        var purchase = await _context.Purchases
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

        return purchase == null ? null :
            new PurchaseDto(purchase.Id, purchase.BuyerId, purchase.OfferId, purchase.PurchaseDate, purchase.Amount, purchase.Status, purchase.IsActive);
    }
}

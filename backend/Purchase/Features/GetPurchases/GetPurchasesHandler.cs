using MediatR;
using Microsoft.EntityFrameworkCore;
using Purchase.Data;

namespace Purchase.Features.GetPurchases;

public record GetPurchasesQuery : IRequest<IEnumerable<PurchaseDto>>;

public record PurchaseDto(int Id, int BuyerId, int OfferId, DateTime PurchaseDate, decimal Amount, string Status, bool IsActive);

public class GetPurchasesHandler : IRequestHandler<GetPurchasesQuery, IEnumerable<PurchaseDto>>
{
    private readonly PurchasesDbContext _context;

    public GetPurchasesHandler(PurchasesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PurchaseDto>> Handle(GetPurchasesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Purchases
            .Where(p => p.IsActive)
            .Select(p => new PurchaseDto(p.Id, p.BuyerId, p.OfferId, p.PurchaseDate, p.Amount, p.Status, p.IsActive))
            .ToListAsync(cancellationToken);
    }
}

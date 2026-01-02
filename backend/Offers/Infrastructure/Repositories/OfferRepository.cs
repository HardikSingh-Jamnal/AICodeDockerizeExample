using Microsoft.EntityFrameworkCore;
using Offers.Domain.Entities;
using Offers.Domain.Enums;
using Offers.Infrastructure.Data;

namespace Offers.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Offer entity operations.
/// </summary>
public class OfferRepository : IOfferRepository
{
    private readonly OffersDbContext _context;

    public OfferRepository(OffersDbContext context)
    {
        _context = context;
    }

    public async Task<Offer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OfferId == offerId, cancellationToken);
    }

    public async Task<IEnumerable<Offer>> GetAllAsync(
        int? sellerId = null,
        OfferStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Offers.AsNoTracking();

        if (sellerId.HasValue)
            query = query.Where(o => o.SellerId == sellerId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(
        int? sellerId = null,
        OfferStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Offers.AsQueryable();

        if (sellerId.HasValue)
            query = query.Where(o => o.SellerId == sellerId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int sellerId, string vin, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .AnyAsync(o => o.SellerId == sellerId && o.Vin == vin, cancellationToken);
    }

    public async Task<Offer> AddAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        _context.Offers.Add(offer);
        await _context.SaveChangesAsync(cancellationToken);
        return offer;
    }

    public async Task UpdateAsync(Offer offer, CancellationToken cancellationToken = default)
    {
        _context.Offers.Update(offer);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

using Offers.Domain.Entities;
using Offers.Domain.Enums;

namespace Offers.Infrastructure.Repositories;

/// <summary>
/// Interface for offer repository operations.
/// </summary>
public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Offer>> GetAllAsync(Guid? sellerId = null, OfferStatus? status = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(Guid? sellerId = null, OfferStatus? status = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid sellerId, string vin, CancellationToken cancellationToken = default);
    Task<Offer> AddAsync(Offer offer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Offer offer, CancellationToken cancellationToken = default);
}

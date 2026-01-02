using MediatR;
using Offers.Domain.Enums;
using Offers.Features.GetOfferById;
using Offers.Infrastructure.Repositories;

namespace Offers.Features.GetOffers;

/// <summary>
/// Query to get a paginated list of offers with optional filters.
/// </summary>
public record GetOffersQuery(
    int? SellerId = null,
    OfferStatus? Status = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<GetOffersResult>;

/// <summary>
/// Result containing paginated offers.
/// </summary>
public record GetOffersResult(
    IEnumerable<OfferDto> Offers,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// Handler for getting a paginated list of offers.
/// </summary>
public class GetOffersHandler : IRequestHandler<GetOffersQuery, GetOffersResult>
{
    private readonly IOfferRepository _offerRepository;

    public GetOffersHandler(IOfferRepository offerRepository)
    {
        _offerRepository = offerRepository;
    }

    public async Task<GetOffersResult> Handle(GetOffersQuery request, CancellationToken cancellationToken)
    {
        // Ensure valid pagination parameters
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var offers = await _offerRepository.GetAllAsync(
            request.SellerId,
            request.Status,
            page,
            pageSize,
            cancellationToken);

        var totalCount = await _offerRepository.GetTotalCountAsync(
            request.SellerId,
            request.Status,
            cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var offerDtos = offers.Select(offer => new OfferDto
        {
            OfferId = offer.OfferId,
            SellerId = offer.SellerId,
            Vin = offer.Vin,
            Make = offer.Make,
            Model = offer.Model,
            Year = offer.Year,
            OfferAmount = offer.OfferAmount,
            Location = offer.Location,
            Condition = offer.Condition,
            Status = offer.Status.ToString(),
            CreatedAt = offer.CreatedAt,
            UpdatedAt = offer.UpdatedAt
        });

        return new GetOffersResult(offerDtos, totalCount, page, pageSize, totalPages);
    }
}

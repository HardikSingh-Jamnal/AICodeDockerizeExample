using MediatR;
using Offers.Domain.Enums;
using Offers.Domain.ValueObjects;
using Offers.Infrastructure.Repositories;

namespace Offers.Features.GetOfferById;

/// <summary>
/// Query to get an offer by its ID.
/// </summary>
public record GetOfferByIdQuery(int OfferId) : IRequest<OfferDto?>;

/// <summary>
/// Data transfer object for an offer.
/// </summary>
public record OfferDto
{
    public int OfferId { get; init; }
    public int SellerId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal OfferAmount { get; init; }
    public Location Location { get; init; } = new();
    public Condition Condition { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Handler for getting an offer by ID.
/// </summary>
public class GetOfferByIdHandler : IRequestHandler<GetOfferByIdQuery, OfferDto?>
{
    private readonly IOfferRepository _offerRepository;

    public GetOfferByIdHandler(IOfferRepository offerRepository)
    {
        _offerRepository = offerRepository;
    }

    public async Task<OfferDto?> Handle(GetOfferByIdQuery request, CancellationToken cancellationToken)
    {
        var offer = await _offerRepository.GetByIdAsync(request.OfferId, cancellationToken);

        if (offer == null)
            return null;

        return new OfferDto
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
        };
    }
}

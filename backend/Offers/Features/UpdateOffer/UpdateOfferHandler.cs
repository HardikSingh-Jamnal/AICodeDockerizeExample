using FluentValidation;
using MediatR;
using Offers.Domain.ValueObjects;
using Offers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Offers.Features.UpdateOffer;

/// <summary>
/// Command to update an existing offer.
/// Only OfferAmount, Location, and Condition can be updated.
/// </summary>
public record UpdateOfferCommand : IRequest<UpdateOfferResult>
{
    public int OfferId { get; init; }
    public decimal? OfferAmount { get; init; }
    public Location? Location { get; init; }
    public Condition? Condition { get; init; }
}

/// <summary>
/// Result of updating an offer.
/// </summary>
public record UpdateOfferResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Validator for UpdateOfferCommand.
/// </summary>
public class UpdateOfferValidator : AbstractValidator<UpdateOfferCommand>
{
    public UpdateOfferValidator()
    {
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithMessage("Offer ID is required");

        When(x => x.OfferAmount.HasValue, () =>
        {
            RuleFor(x => x.OfferAmount!.Value)
                .GreaterThan(0)
                .WithMessage("Offer amount must be positive");
        });

        When(x => x.Location != null, () =>
        {
            RuleFor(x => x.Location!.City)
                .NotEmpty()
                .WithMessage("City is required");

            RuleFor(x => x.Location!.State)
                .NotEmpty()
                .WithMessage("State is required");

            RuleFor(x => x.Location!.ZipCode)
                .NotEmpty()
                .WithMessage("Zip code is required")
                .Matches(@"^\d{5}(-\d{4})?$")
                .When(x => !string.IsNullOrEmpty(x.Location?.ZipCode))
                .WithMessage("Zip code must be in format 12345 or 12345-6789");
        });

        When(x => x.Condition != null, () =>
        {
            RuleFor(x => x.Condition!.Mileage)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Mileage cannot be negative");
        });

        // At least one field must be provided for update
        RuleFor(x => x)
            .Must(x => x.OfferAmount.HasValue || x.Location != null || x.Condition != null)
            .WithMessage("At least one field must be provided for update");
    }
}

/// <summary>
/// Handler for updating an offer.
/// Enforces business rule that only Active or Pending offers can be updated.
/// </summary>
public class UpdateOfferHandler : IRequestHandler<UpdateOfferCommand, UpdateOfferResult>
{
    private readonly OffersDbContext _context;

    public UpdateOfferHandler(OffersDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateOfferResult> Handle(UpdateOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await _context.Offers
            .FirstOrDefaultAsync(o => o.OfferId == request.OfferId, cancellationToken);

        if (offer == null)
        {
            return new UpdateOfferResult(false, "Offer not found");
        }

        if (!offer.CanBeUpdated())
        {
            return new UpdateOfferResult(false, 
                $"Offer cannot be updated because its status is {offer.Status}. Only Active or Pending offers can be updated.");
        }

        // Apply updates via domain method
        var updated = offer.Update(request.OfferAmount, request.Location, request.Condition);

        if (!updated)
        {
            return new UpdateOfferResult(false, "Failed to update offer");
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateOfferResult(true);
    }
}

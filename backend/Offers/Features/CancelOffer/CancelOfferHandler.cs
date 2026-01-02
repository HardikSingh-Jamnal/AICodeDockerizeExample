using System.Text.Json;
using MediatR;
using Offers.Domain.Entities;
using Offers.Domain.Events;
using Offers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Offers.Features.CancelOffer;

/// <summary>
/// Command to cancel an existing offer.
/// </summary>
public record CancelOfferCommand(Guid OfferId) : IRequest<CancelOfferResult>;

/// <summary>
/// Result of cancelling an offer.
/// </summary>
public record CancelOfferResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Handler for cancelling an offer.
/// Enforces business rule that only Active or Pending offers can be cancelled.
/// Publishes OfferCancelled event via transactional outbox.
/// </summary>
public class CancelOfferHandler : IRequestHandler<CancelOfferCommand, CancelOfferResult>
{
    private readonly OffersDbContext _context;

    public CancelOfferHandler(OffersDbContext context)
    {
        _context = context;
    }

    public async Task<CancelOfferResult> Handle(CancelOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await _context.Offers
            .FirstOrDefaultAsync(o => o.OfferId == request.OfferId, cancellationToken);

        if (offer == null)
        {
            return new CancelOfferResult(false, "Offer not found");
        }

        var previousStatus = offer.Status.ToString();

        if (!offer.CanBeCancelled())
        {
            return new CancelOfferResult(false, 
                $"Offer cannot be cancelled because its status is {offer.Status}. Only Active or Pending offers can be cancelled.");
        }

        // Cancel via domain method
        var cancelled = offer.Cancel();

        if (!cancelled)
        {
            return new CancelOfferResult(false, "Failed to cancel offer");
        }

        // Create domain event
        var offerCancelledEvent = new OfferCancelledEvent
        {
            OfferId = offer.OfferId,
            SellerId = offer.SellerId,
            Vin = offer.Vin,
            PreviousStatus = previousStatus,
            NewStatus = offer.Status.ToString(),
            CancelledAt = offer.UpdatedAt ?? DateTime.UtcNow,
            EventTimestamp = DateTime.UtcNow
        };

        // Create outbox message for reliable event publishing
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = offerCancelledEvent.EventType,
            Payload = JsonSerializer.Serialize(offerCancelledEvent),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return new CancelOfferResult(true);
    }
}

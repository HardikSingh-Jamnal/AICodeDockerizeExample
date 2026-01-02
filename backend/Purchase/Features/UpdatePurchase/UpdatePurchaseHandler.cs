using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Purchase.Data;
using Purchase.Entities;
using Purchase.Events;

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

        // Create domain event
        var purchaseUpdatedEvent = new PurchaseUpdatedEvent
        {
            PurchaseId = purchase.Id,
            BuyerId = purchase.BuyerId,
            OfferId = purchase.OfferId,
            Amount = purchase.Amount,
            Status = purchase.Status,
            PurchaseDate = purchase.PurchaseDate,
            CreatedAt = purchase.CreatedAt,
            UpdatedAt = purchase.UpdatedAt,
            EventTimestamp = DateTime.UtcNow
        };

        // Create outbox message for reliable event publishing
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = purchaseUpdatedEvent.EventType,
            Payload = JsonSerializer.Serialize(purchaseUpdatedEvent),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

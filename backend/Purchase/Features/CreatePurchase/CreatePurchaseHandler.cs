using System.Text.Json;
using FluentValidation;
using MediatR;
using Purchase.Data;
using Purchase.Entities;
using Purchase.Events;

namespace Purchase.Features.CreatePurchase;

public record CreatePurchaseCommand(int BuyerId, int OfferId, decimal Amount, string Status) : IRequest<int>;

public class CreatePurchaseValidator : AbstractValidator<CreatePurchaseCommand>
{
    public CreatePurchaseValidator()
    {
        RuleFor(x => x.BuyerId).GreaterThan(0);
        RuleFor(x => x.OfferId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
    }
}

public class CreatePurchaseHandler : IRequestHandler<CreatePurchaseCommand, int>
{
    private readonly PurchasesDbContext _context;

    public CreatePurchaseHandler(PurchasesDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreatePurchaseCommand request, CancellationToken cancellationToken)
    {
        var purchase = new Entities.Purchase
        {
            BuyerId = request.BuyerId,
            OfferId = request.OfferId,
            Amount = request.Amount,
            Status = request.Status,
            PurchaseDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync(cancellationToken);

        // Create domain event
        var purchaseCreatedEvent = new PurchaseCreatedEvent
        {
            PurchaseId = purchase.Id,
            BuyerId = purchase.BuyerId,
            OfferId = purchase.OfferId,
            Amount = purchase.Amount,
            Status = purchase.Status,
            PurchaseDate = purchase.PurchaseDate,
            CreatedAt = purchase.CreatedAt,
            EventTimestamp = DateTime.UtcNow
        };

        // Create outbox message for reliable event publishing
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = purchaseCreatedEvent.EventType,
            Payload = JsonSerializer.Serialize(purchaseCreatedEvent),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);

        return purchase.Id;
    }
}

using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Transport.Data;
using Transport.Entities;
using Transport.Events;

namespace Transport.Features.UpdateTransport;

public record UpdateTransportCommand(
    int TransportId,
    int CarrierId,
    int PurchaseId,
    string PickupStreet,
    string PickupCity,
    string PickupStateCode,
    string PickupCountry,
    string PickupZipCode,
    string DeliveryStreet,
    string DeliveryCity,
    string DeliveryStateCode,
    string DeliveryCountry,
    string DeliveryZipCode,
    DateTime ScheduleDate,
    string Status,
    string? Notes,
    decimal? EstimatedCost,
    decimal? ActualCost
) : IRequest<bool>;

public class UpdateTransportValidator : AbstractValidator<UpdateTransportCommand>
{
    public UpdateTransportValidator()
    {
        RuleFor(x => x.TransportId).GreaterThan(0);
        RuleFor(x => x.CarrierId).GreaterThan(0).WithMessage("CarrierId must be greater than 0");
        RuleFor(x => x.PurchaseId).GreaterThan(0).WithMessage("PurchaseId must be greater than 0");
        RuleFor(x => x.PickupStreet).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PickupCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PickupStateCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.PickupCountry).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PickupZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DeliveryStreet).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DeliveryCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DeliveryStateCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.DeliveryCountry).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DeliveryZipCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0).When(x => x.EstimatedCost.HasValue);
        RuleFor(x => x.ActualCost).GreaterThanOrEqualTo(0).When(x => x.ActualCost.HasValue);
    }
}

public class UpdateTransportHandler : IRequestHandler<UpdateTransportCommand, bool>
{
    private readonly TransportDbContext _context;

    public UpdateTransportHandler(TransportDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateTransportCommand request, CancellationToken cancellationToken)
    {
        var transport = await _context.Transports
            .FirstOrDefaultAsync(t => t.TransportId == request.TransportId, cancellationToken);

        if (transport == null)
            return false;

        transport.CarrierId = request.CarrierId;
        transport.PurchaseId = request.PurchaseId;
        transport.PickupStreet = request.PickupStreet;
        transport.PickupCity = request.PickupCity;
        transport.PickupStateCode = request.PickupStateCode;
        transport.PickupCountry = request.PickupCountry;
        transport.PickupZipCode = request.PickupZipCode;
        transport.DeliveryStreet = request.DeliveryStreet;
        transport.DeliveryCity = request.DeliveryCity;
        transport.DeliveryStateCode = request.DeliveryStateCode;
        transport.DeliveryCountry = request.DeliveryCountry;
        transport.DeliveryZipCode = request.DeliveryZipCode;
        transport.ScheduleDate = request.ScheduleDate;
        transport.Status = request.Status;
        transport.Notes = request.Notes ?? string.Empty;
        transport.EstimatedCost = request.EstimatedCost;
        transport.ActualCost = request.ActualCost;
        transport.UpdatedAt = DateTime.UtcNow;

        // Create domain event
        var transportUpdatedEvent = new TransportUpdatedEvent
        {
            TransportId = transport.TransportId,
            CarrierId = transport.CarrierId,
            PurchaseId = transport.PurchaseId,
            PickupCity = transport.PickupCity,
            PickupStateCode = transport.PickupStateCode,
            DeliveryCity = transport.DeliveryCity,
            DeliveryStateCode = transport.DeliveryStateCode,
            ScheduleDate = transport.ScheduleDate,
            Status = transport.Status,
            EstimatedCost = transport.EstimatedCost,
            ActualCost = transport.ActualCost,
            CreatedAt = transport.CreatedAt,
            UpdatedAt = transport.UpdatedAt,
            EventTimestamp = DateTime.UtcNow
        };

        // Create outbox message for reliable event publishing
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = transportUpdatedEvent.EventType,
            Payload = JsonSerializer.Serialize(transportUpdatedEvent),
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(outboxMessage);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
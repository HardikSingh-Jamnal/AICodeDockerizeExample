using FluentValidation;
using MediatR;
using Transport.Data;
using Transport.Entities;

namespace Transport.Features.CreateTransport;

public record CreateTransportCommand(
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
    decimal? EstimatedCost
) : IRequest<int>;

public class CreateTransportValidator : AbstractValidator<CreateTransportCommand>
{
    public CreateTransportValidator()
    {
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
        RuleFor(x => x.ScheduleDate).GreaterThan(DateTime.UtcNow.AddMinutes(-1)).WithMessage("Schedule date cannot be in the past");
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0).When(x => x.EstimatedCost.HasValue);
    }
}

public class CreateTransportHandler : IRequestHandler<CreateTransportCommand, int>
{
    private readonly TransportDbContext _context;

    public CreateTransportHandler(TransportDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateTransportCommand request, CancellationToken cancellationToken)
    {
        var transport = new TransportEntity
        {
            CarrierId = request.CarrierId,
            PurchaseId = request.PurchaseId,
            PickupStreet = request.PickupStreet,
            PickupCity = request.PickupCity,
            PickupStateCode = request.PickupStateCode,
            PickupCountry = request.PickupCountry,
            PickupZipCode = request.PickupZipCode,
            DeliveryStreet = request.DeliveryStreet,
            DeliveryCity = request.DeliveryCity,
            DeliveryStateCode = request.DeliveryStateCode,
            DeliveryCountry = request.DeliveryCountry,
            DeliveryZipCode = request.DeliveryZipCode,
            ScheduleDate = request.ScheduleDate,
            Status = request.Status,
            Notes = request.Notes ?? string.Empty,
            EstimatedCost = request.EstimatedCost
        };

        _context.Transports.Add(transport);
        await _context.SaveChangesAsync(cancellationToken);

        return transport.TransportId;
    }
}
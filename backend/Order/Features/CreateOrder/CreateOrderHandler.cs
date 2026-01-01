using Contracts;
using FluentValidation;
using MassTransit;
using MediatR;
using Order.Data;
using Order.Entities;

namespace Order.Features.CreateOrder;

public record CreateOrderCommand(int CustomerId, string CustomerName, string CustomerEmail, string ShippingAddress, List<OrderItemDto> OrderItems) : IRequest<int>;

public record OrderItemDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OrderItems).NotEmpty();
        RuleForEach(x => x.OrderItems).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).GreaterThan(0);
            item.RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
            item.RuleFor(x => x.UnitPrice).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly OrderDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateOrderHandler(OrderDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new OrderEntity
        {
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            ShippingAddress = request.ShippingAddress,
            TotalAmount = request.OrderItems.Sum(item => item.UnitPrice * item.Quantity),
            OrderItems = request.OrderItems.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _publishEndpoint.Publish(new OrderPlaced
        {
            OrderId = Guid.NewGuid(),
            ProductIds = order.OrderItems.Select(x=> new Guid($"{x.ProductId:D8}-0000-0000-0000-000000000000")).ToArray(),
            TotalAmount = order.TotalAmount,
            OrderDate = DateTime.UtcNow
        }, cancellationToken);
        
        return order.Id;
    }
}
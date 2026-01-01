using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Data;
using Order.Entities;

namespace Order.Features.GetOrders;

public record GetOrdersQuery : IRequest<IEnumerable<OrderDto>>;

public record OrderDto(int Id, int CustomerId, string CustomerName, string CustomerEmail, decimal TotalAmount, OrderStatus Status, string ShippingAddress, DateTime OrderDate, DateTime? ShippedDate, DateTime? DeliveredDate);

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly OrderDbContext _context;

    public GetOrdersHandler(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Select(o => new OrderDto(o.Id, o.CustomerId, o.CustomerName, o.CustomerEmail, o.TotalAmount, o.Status, o.ShippingAddress, o.OrderDate, o.ShippedDate, o.DeliveredDate))
            .ToListAsync(cancellationToken);
    }
}
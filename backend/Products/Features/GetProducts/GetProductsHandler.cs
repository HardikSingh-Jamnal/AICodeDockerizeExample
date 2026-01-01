using MediatR;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Entities;

namespace Products.Features.GetProducts;

public record GetProductsQuery : IRequest<IEnumerable<ProductDto>>;

public record ProductDto(int Id, string Name, string Description, decimal Price, int StockQuantity, string Category, bool IsActive);

public class GetProductsHandler : IRequestHandler<GetProductsQuery, IEnumerable<ProductDto>>
{
    private readonly ProductsDbContext _context;

    public GetProductsHandler(ProductsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.StockQuantity, p.Category, p.IsActive))
            .ToListAsync(cancellationToken);
    }
}
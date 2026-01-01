using MediatR;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Entities;

namespace Products.Features.GetProductById;

public record GetProductByIdQuery(int Id) : IRequest<ProductDto?>;

public record ProductDto(int Id, string Name, string Description, decimal Price, int StockQuantity, string Category, bool IsActive);

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly ProductsDbContext _context;

    public GetProductByIdHandler(ProductsDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

        return product == null ? null : 
            new ProductDto(product.Id, product.Name, product.Description, product.Price, product.StockQuantity, product.Category, product.IsActive);
    }
}
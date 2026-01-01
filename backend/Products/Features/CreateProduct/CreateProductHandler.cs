using FluentValidation;
using MediatR;
using Products.Data;
using Products.Entities;

namespace Products.Features.CreateProduct;

public record CreateProductCommand(string Name, string Description, decimal Price, int StockQuantity, string Category) : IRequest<int>;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).MaximumLength(100);
    }
}

public class CreateProductHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly ProductsDbContext _context;

    public CreateProductHandler(ProductsDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Features.GetProducts;
using Products.Features.GetProductById;
using Products.Features.CreateProduct;
using Products.Features.UpdateProduct;
using Products.Features.DeleteProduct;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Add Entity Framework
builder.Services.AddDbContext<ProductsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                        "Server=localhost;Database=ProductsDB;Trusted_Connection=true;TrustServerCertificate=true;"));

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Products endpoints
app.MapGet("/products", async (IMediator mediator) =>
{
    return await mediator.Send(new GetProductsQuery());
})
.WithName("GetProducts");

app.MapGet("/products/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductByIdQuery(id));
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetProductById");

app.MapPost("/products", async (CreateProductCommand command, IMediator mediator) =>
{
    var validator = new CreateProductValidator();
    var validationResult = await validator.ValidateAsync(command);
    
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);
    
    var id = await mediator.Send(command);
    return Results.Created($"/products/{id}", new { Id = id });
})
.WithName("CreateProduct");

app.MapPut("/products/{id:int}", async (int id, UpdateProductCommand command, IMediator mediator) =>
{
    if (id != command.Id)
        return Results.BadRequest("ID mismatch");
        
    var validator = new UpdateProductValidator();
    var validationResult = await validator.ValidateAsync(command);
    
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);
    
    var result = await mediator.Send(command);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("UpdateProduct");

app.MapDelete("/products/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new DeleteProductCommand(id));
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteProduct");

app.Run();
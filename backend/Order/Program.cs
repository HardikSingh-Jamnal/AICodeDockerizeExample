using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Data;
using Order.Features.GetOrders;
using Order.Features.CreateOrder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Add Entity Framework
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                        "Server=localhost;Database=OrderDB;Trusted_Connection=true;TrustServerCertificate=true;"));

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

// Order endpoints
app.MapGet("/orders", async (IMediator mediator) =>
{
    return await mediator.Send(new GetOrdersQuery());
})
.WithName("GetOrders");

app.MapPost("/orders", async (CreateOrderCommand command, IMediator mediator) =>
{
    var validator = new CreateOrderValidator();
    var validationResult = await validator.ValidateAsync(command);
    
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);
    
    var id = await mediator.Send(command);
    return Results.Created($"/orders/{id}", new { Id = id });
})
.WithName("CreateOrder");

app.Run();

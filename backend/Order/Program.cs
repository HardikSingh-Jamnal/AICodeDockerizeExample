using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Data;
using Order.Features.GetOrders;
using Order.Features.CreateOrder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Entity Framework
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Host=localhost;Port=5433;Database=order_db;Username=user;Password=password"));

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Apply migrations and seed database
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

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

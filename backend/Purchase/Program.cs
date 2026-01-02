using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Purchase.Data;
using Purchase.Features.GetPurchases;
using Purchase.Features.GetPurchaseById;
using Purchase.Features.CreatePurchase;
using Purchase.Features.UpdatePurchase;
using Purchase.Features.DeletePurchase;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

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
builder.Services.AddDbContext<PurchasesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=guest"));

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
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchasesDbContext>();
        dbContext.Database.EnsureCreated();
    }
}

app.UseCors("AllowFrontend");

// Purchases endpoints
app.MapGet("/purchases", async (IMediator mediator) =>
{
    return await mediator.Send(new GetPurchasesQuery());
})
.WithName("GetPurchases");

app.MapGet("/purchases/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetPurchaseByIdQuery(id));
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetPurchaseById");

app.MapPost("/purchases", async (CreatePurchaseCommand command, IMediator mediator) =>
{
    var validator = new CreatePurchaseValidator();
    var validationResult = await validator.ValidateAsync(command);

    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);

    var id = await mediator.Send(command);
    return Results.Created($"/purchases/{id}", new { Id = id });
})
.WithName("CreatePurchase");

app.MapPut("/purchases/{id:int}", async (int id, UpdatePurchaseCommand command, IMediator mediator) =>
{
    // Set the Id from the route parameter
    var commandWithId = command with { Id = id };

    var validator = new UpdatePurchaseValidator();
    var validationResult = await validator.ValidateAsync(commandWithId);

    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);

    var result = await mediator.Send(commandWithId);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("UpdatePurchase");

app.MapDelete("/purchases/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new DeletePurchaseCommand(id));
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("DeletePurchase");

app.Run();

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Offers.Domain.Enums;
using Offers.Domain.ValueObjects;
using Offers.Features.CreateOffer;
using Offers.Features.GetOfferById;
using Offers.Features.GetOffers;
using Offers.Features.UpdateOffer;
using Offers.Features.CancelOffer;
using Offers.Infrastructure.Data;
using Offers.Infrastructure.Repositories;
using Offers.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Offers API",
        Version = "v1",
        Description = "Production-grade Offer Service for vehicle offers management",
        Contact = new OpenApiContact
        {
            Name = "Offers Service Team"
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<OffersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Host=localhost;Port=5432;Database=offers_db;Username=user;Password=password"));

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add Repositories
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Add RabbitMQ Publisher
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

// Add Outbox Processor Background Service
builder.Services.AddHostedService<OutboxProcessor>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection") ??
        "Host=localhost;Port=5432;Database=offers_db;Username=user;Password=password",
        name: "postgresql",
        tags: new[] { "db", "sql", "postgresql" })
    .AddRabbitMQ(
        builder.Configuration["MessageBroker:ConnectionString"] ??
        "amqp://admin:password@localhost:5672",
        name: "rabbitmq",
        tags: new[] { "messaging", "rabbitmq" });

var app = builder.Build();

// Apply migrations and seed database
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OffersDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Offers API V1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowFrontend");

// Health check endpoint
app.MapHealthChecks("/health");

// API Endpoints
var offersApi = app.MapGroup("/api/offers")
    .WithTags("Offers")
    .WithOpenApi();

// POST /api/offers - Create a new offer
offersApi.MapPost("/", async (CreateOfferRequest request, IMediator mediator, IValidator<CreateOfferCommand> validator) =>
{
    var command = new CreateOfferCommand(
        request.SellerId,
        request.Vin,
        request.Make,
        request.Model,
        request.Year,
        request.OfferAmount,
        request.Location,
        request.Condition
    );

    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
    }

    var result = await mediator.Send(command);

    if (!result.Success)
    {
        return Results.Conflict(new { Error = result.ErrorMessage });
    }

    return Results.Created($"/api/offers/{result.OfferId}", new { OfferId = result.OfferId });
})
.WithName("CreateOffer")
.WithDescription("Create a new vehicle offer")
.Produces<object>(StatusCodes.Status201Created)
.Produces<object>(StatusCodes.Status400BadRequest)
.Produces<object>(StatusCodes.Status409Conflict);

// GET /api/offers - List offers with optional filters
offersApi.MapGet("/", async (
    int? sellerId,
    OfferStatus? status,
    int page,
    int pageSize,
    IMediator mediator) =>
{
    var query = new GetOffersQuery(sellerId, status, page > 0 ? page : 1, pageSize > 0 ? pageSize : 20);
    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.WithName("GetOffers")
.WithDescription("Get a paginated list of offers with optional filters")
.Produces<GetOffersResult>(StatusCodes.Status200OK);

// GET /api/offers/{id} - Get offer by ID
offersApi.MapGet("/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOfferByIdQuery(id));
    return result != null ? Results.Ok(result) : Results.NotFound(new { Error = "Offer not found" });
})
.WithName("GetOfferById")
.WithDescription("Get a specific offer by ID")
.Produces<OfferDto>(StatusCodes.Status200OK)
.Produces<object>(StatusCodes.Status404NotFound);

// PUT /api/offers/{id} - Update an offer
offersApi.MapPut("/{id:int}", async (int id, UpdateOfferRequest request, IMediator mediator, IValidator<UpdateOfferCommand> validator) =>
{
    var command = new UpdateOfferCommand
    {
        OfferId = id,
        OfferAmount = request.OfferAmount,
        Location = request.Location,
        Condition = request.Condition
    };

    var validationResult = await validator.ValidateAsync(command);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new { Errors = validationResult.Errors.Select(e => e.ErrorMessage) });
    }

    var result = await mediator.Send(command);

    if (!result.Success)
    {
        return result.ErrorMessage?.Contains("not found") == true
            ? Results.NotFound(new { Error = result.ErrorMessage })
            : Results.BadRequest(new { Error = result.ErrorMessage });
    }

    return Results.NoContent();
})
.WithName("UpdateOffer")
.WithDescription("Update an existing offer (only Active or Pending offers)")
.Produces(StatusCodes.Status204NoContent)
.Produces<object>(StatusCodes.Status400BadRequest)
.Produces<object>(StatusCodes.Status404NotFound);

// POST /api/offers/{id}/cancel - Cancel an offer
offersApi.MapPost("/{id:int}/cancel", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new CancelOfferCommand(id));

    if (!result.Success)
    {
        return result.ErrorMessage?.Contains("not found") == true
            ? Results.NotFound(new { Error = result.ErrorMessage })
            : Results.BadRequest(new { Error = result.ErrorMessage });
    }

    return Results.NoContent();
})
.WithName("CancelOffer")
.WithDescription("Cancel an existing offer (only Active or Pending offers)")
.Produces(StatusCodes.Status204NoContent)
.Produces<object>(StatusCodes.Status400BadRequest)
.Produces<object>(StatusCodes.Status404NotFound);

app.Run();

// Request DTOs for cleaner API contracts
public record CreateOfferRequest(
    int SellerId,
    string Vin,
    string Make,
    string Model,
    int Year,
    decimal OfferAmount,
    Location Location,
    Condition Condition
);

public record UpdateOfferRequest(
    decimal? OfferAmount,
    Location? Location,
    Condition? Condition
);

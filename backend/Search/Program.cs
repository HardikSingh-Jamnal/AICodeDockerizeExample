using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;
using Search.Features.Autocomplete;
using Search.Features.UnifiedSearch;
using Search.Infrastructure.Consumers;
using Search.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Search API",
        Version = "v1",
        Description = "Centralized Search Service for the automotive marketplace platform. Provides unified search across Offers, Purchases, and Transports with role-based access control.",
        Contact = new OpenApiContact
        {
            Name = "Search Service Team"
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

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add Elasticsearch Service
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();

// Add RabbitMQ Event Consumers
builder.Services.AddHostedService<OfferEventConsumer>();
builder.Services.AddHostedService<PurchaseEventConsumer>();
builder.Services.AddHostedService<TransportEventConsumer>();

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Ensure Elasticsearch index exists on startup
using (var scope = app.Services.CreateScope())
{
    var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
    try
    {
        await elasticsearchService.EnsureIndexExistsAsync();
        app.Logger.LogInformation("Elasticsearch index initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to initialize Elasticsearch index on startup. Will retry when needed.");
    }
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Search API V1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowFrontend");

// Health check endpoint
app.MapHealthChecks("/health");

// API Endpoints
var searchApi = app.MapGroup("/api/search")
    .WithTags("Search")
    .WithOpenApi();

// GET /api/search - Unified search across all entities
searchApi.MapGet("/", async (
    string? q,
    string? entityType,
    string? userId,
    string? userRole,
    int? page,
    int? pageSize,
    string? sortBy,
    bool? sortDescending,
    IMediator mediator) =>
{
    var query = new UnifiedSearchQuery
    {
        Query = q ?? string.Empty,
        EntityType = entityType,
        UserId = userId,
        UserRole = userRole,
        Page = page ?? 1,
        PageSize = pageSize ?? 20,
        SortBy = sortBy,
        SortDescending = sortDescending ?? true
    };

    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.WithName("UnifiedSearch")
.WithDescription("Search across all entity types (Offers, Purchases, Transports) with optional filters and role-based access control")
.Produces<SearchResult>(StatusCodes.Status200OK);

// GET /api/search/autocomplete - Autocomplete suggestions
searchApi.MapGet("/autocomplete", async (
    string q,
    int? maxSuggestions,
    IMediator mediator) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
    {
        return Results.Ok(Enumerable.Empty<AutocompleteSuggestion>());
    }

    var query = new AutocompleteQuery
    {
        Query = q,
        MaxSuggestions = maxSuggestions ?? 10
    };

    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.WithName("Autocomplete")
.WithDescription("Get autocomplete suggestions based on partial input")
.Produces<IEnumerable<AutocompleteSuggestion>>(StatusCodes.Status200OK);

// GET /api/search/offers - Search only offers
searchApi.MapGet("/offers", async (
    string? q,
    string? userId,
    string? userRole,
    int? page,
    int? pageSize,
    IMediator mediator) =>
{
    var query = new UnifiedSearchQuery
    {
        Query = q ?? string.Empty,
        EntityType = "Offer",
        UserId = userId,
        UserRole = userRole,
        Page = page ?? 1,
        PageSize = pageSize ?? 20
    };

    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.WithName("SearchOffers")
.WithDescription("Search only within Offers")
.Produces<SearchResult>(StatusCodes.Status200OK);

// GET /api/search/purchases - Search only purchases
searchApi.MapGet("/purchases", async (
    string? q,
    string? userId,
    string? userRole,
    int? page,
    int? pageSize,
    IMediator mediator) =>
{
    var query = new UnifiedSearchQuery
    {
        Query = q ?? string.Empty,
        EntityType = "Purchase",
        UserId = userId,
        UserRole = userRole,
        Page = page ?? 1,
        PageSize = pageSize ?? 20
    };

    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.WithName("SearchPurchases")
.WithDescription("Search only within Purchases")
.Produces<SearchResult>(StatusCodes.Status200OK);

// GET /api/search/transports - Search only transports
searchApi.MapGet("/transports", async (
    string? q,
    string? userId,
    string? userRole,
    int? page,
    int? pageSize,
    IMediator mediator) =>
{
    var query = new UnifiedSearchQuery
    {
        Query = q ?? string.Empty,
        EntityType = "Transport",
        UserId = userId,
        UserRole = userRole,
        Page = page ?? 1,
        PageSize = pageSize ?? 20
    };

    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.WithName("SearchTransports")
.WithDescription("Search only within Transports")
.Produces<SearchResult>(StatusCodes.Status200OK);

// POST /api/search/reindex - Trigger reindexing (admin only)
searchApi.MapPost("/reindex", async (IElasticsearchService elasticsearchService) =>
{
    try
    {
        await elasticsearchService.EnsureIndexExistsAsync();
        return Results.Ok(new { Message = "Index recreated successfully. Documents will be reindexed as events are received." });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to recreate index: {ex.Message}");
    }
})
.WithName("ReindexSearch")
.WithDescription("Recreate the search index (admin operation)")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

app.Run();

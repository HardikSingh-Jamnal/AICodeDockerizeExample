using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Transport.Data;
using Transport.Features.GetTransports;
using Transport.Features.GetTransportById;
using Transport.Features.CreateTransport;
using Transport.Features.UpdateTransport;
using Transport.Features.DeleteTransport;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Transport API",
        Version = "v1",
        Description = "Production-grade Transport Service for transport management",
        Contact = new OpenApiContact
        {
            Name = "Transport Service Team"
        }
    });
});

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
builder.Services.AddDbContext<TransportDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=Mtech1"));

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
        var dbContext = scope.ServiceProvider.GetRequiredService<TransportDbContext>();
        dbContext.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transport API V1");
    c.RoutePrefix = string.Empty;
});

app.UseCors("AllowFrontend");

// Transport endpoints
app.MapGet("/transports", async (IMediator mediator) =>
{
    return await mediator.Send(new GetTransportsQuery());
})
.WithName("GetTransports")
.WithOpenApi();

app.MapGet("/transports/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetTransportByIdQuery(id));
    return result != null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetTransportById")
.WithOpenApi();

app.MapPost("/transports", async (CreateTransportCommand command, IMediator mediator) =>
{
    var validator = new CreateTransportValidator();
    var validationResult = await validator.ValidateAsync(command);

    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);

    var id = await mediator.Send(command);
    return Results.Created($"/transports/{id}", new { TransportId = id });
})
.WithName("CreateTransport")
.WithOpenApi();

app.MapPut("/transports/{id:int}", async (int id, UpdateTransportCommand command, IMediator mediator) =>
{
    if (id != command.TransportId)
        return Results.BadRequest("ID mismatch");

    var validator = new UpdateTransportValidator();
    var validationResult = await validator.ValidateAsync(command);

    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);

    var result = await mediator.Send(command);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("UpdateTransport")
.WithOpenApi();

app.MapDelete("/transports/{id:int}", async (int id, IMediator mediator) =>
{
    var result = await mediator.Send(new DeleteTransportCommand(id));
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteTransport")
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
.WithName("HealthCheck")
.WithOpenApi();

app.Run();
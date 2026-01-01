using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Billing.Data;
using Billing.Features.GetBillingRecords;
using Billing.Features.CreateBillingRecord;
using Billing.Features.UpdateBillingStatus;

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
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Host=localhost;Port=5434;Database=billing_db;Username=user;Password=password"));

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
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        dbContext.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

// Billing endpoints
app.MapGet("/billing", async (IMediator mediator) =>
{
    return await mediator.Send(new GetBillingRecordsQuery());
})
.WithName("GetBillingRecords");

app.MapPost("/billing", async (CreateBillingRecordCommand command, IMediator mediator) =>
{
    var validator = new CreateBillingRecordValidator();
    var validationResult = await validator.ValidateAsync(command);
    
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);
    
    var id = await mediator.Send(command);
    return Results.Created($"/billing/{id}", new { Id = id });
})
.WithName("CreateBillingRecord");

app.MapPut("/billing/{id:int}/status", async (int id, UpdateBillingStatusCommand command, IMediator mediator) =>
{
    if (id != command.Id)
        return Results.BadRequest("ID mismatch");
        
    var validator = new UpdateBillingStatusValidator();
    var validationResult = await validator.ValidateAsync(command);
    
    if (!validationResult.IsValid)
        return Results.BadRequest(validationResult.Errors);
    
    var result = await mediator.Send(command);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("UpdateBillingStatus");

app.Run();

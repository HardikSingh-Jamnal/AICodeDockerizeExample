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

// Add Entity Framework
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
                        "Server=localhost;Database=BillingDB;Trusted_Connection=true;TrustServerCertificate=true;"));

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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/products", () =>
{
    var products = new[]
    {
        new { Id = 1, Name = "Product 1", Price = 10.0 },
        new { Id = 2, Name = "Product 2", Price = 20.0 },
        new { Id = 3, Name = "Product 3", Price = 30.0 },
    };
    return products;
})
.WithName("GetProducts");

app.Run();
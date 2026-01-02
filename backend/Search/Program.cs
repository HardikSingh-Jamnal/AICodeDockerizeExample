using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Search.Infrastructure;
using Search.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Search API",
        Version = "v1",
        Description = "Centralized Search Service for automotive marketplace",
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

// Configure Elasticsearch with JsonNetSerializer for proper polymorphic deserialization
var elasticsearchUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
var indexName = builder.Configuration["Elasticsearch:IndexName"] ?? "search_entities";

var pool = new Elasticsearch.Net.SingleNodeConnectionPool(new Uri(elasticsearchUri));
var settings = new ConnectionSettings(pool, JsonNetSerializer.Default)
    .DefaultIndex(indexName)
    .EnableDebugMode()
    .PrettyJson()
    .RequestTimeout(TimeSpan.FromMinutes(2));

builder.Services.AddSingleton<IElasticClient>(new ElasticClient(settings));
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

// Add RabbitMQ Event Consumer
builder.Services.AddHostedService<EventConsumerService>();

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Initialize Elasticsearch index
using (var scope = app.Services.CreateScope())
{
    var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
    try
    {
        await elasticsearchService.CreateIndexIfNotExistsAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Failed to create Elasticsearch index on startup: {Error}. Will retry later.", ex.Message);
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

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

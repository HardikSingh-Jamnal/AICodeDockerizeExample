using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Search.Domain.Entities;
using Search.Domain.Enums;
using Search.Domain.Events;
using Search.Infrastructure.Services;

namespace Search.Infrastructure.Consumers;

/// <summary>
/// Background service that consumes offer events from RabbitMQ and indexes them in Elasticsearch.
/// </summary>
public class OfferEventConsumer : BackgroundService
{
    private readonly ILogger<OfferEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "offers.events";
    private const string QueueName = "search.offer.events";

    public OfferEventConsumer(
        ILogger<OfferEventConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken); // Wait for RabbitMQ to be ready

        try
        {
            InitializeRabbitMq();
            StartConsuming(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ consumer for offers");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void InitializeRabbitMq()
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["MessageBroker:Host"] ?? "localhost",
            UserName = _configuration["MessageBroker:Username"] ?? "admin",
            Password = _configuration["MessageBroker:Password"] ?? "password",
            Port = int.Parse(_configuration["MessageBroker:Port"] ?? "5672"),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange (should already exist from Offers service)
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Declare queue
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind to all offer events
        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: "#");

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        _logger.LogInformation("RabbitMQ consumer initialized for {Queue}", QueueName);
    }

    private void StartConsuming(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventType = ea.BasicProperties.Type ?? ea.RoutingKey;

            try
            {
                _logger.LogInformation("Received offer event: {EventType}", eventType);
                
                await ProcessOfferEventAsync(message, eventType, stoppingToken);
                
                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing offer event: {EventType}", eventType);
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ProcessOfferEventAsync(string message, string eventType, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var offerEvent = JsonSerializer.Deserialize<OfferEvent>(message, options);

        if (offerEvent == null)
        {
            _logger.LogWarning("Failed to deserialize offer event");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

        if (eventType.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) ||
            eventType.Contains("Deleted", StringComparison.OrdinalIgnoreCase))
        {
            var documentId = $"Offer_{offerEvent.OfferId}";
            await elasticsearchService.DeleteDocumentAsync(documentId, cancellationToken);
            return;
        }

        var document = MapToSearchDocument(offerEvent);
        await elasticsearchService.IndexDocumentAsync(document, cancellationToken);
    }

    private SearchDocument MapToSearchDocument(OfferEvent offerEvent)
    {
        var location = offerEvent.Location;
        var locationString = location != null
            ? $"{location.City}, {location.StateCode}, {location.Country}"
            : string.Empty;

        var keywords = new List<string>
        {
            offerEvent.Vin,
            offerEvent.Make,
            offerEvent.Model,
            offerEvent.Status,
            offerEvent.Year.ToString()
        };

        if (location != null)
        {
            keywords.Add(location.City);
            keywords.Add(location.StateCode);
            keywords.Add(location.ZipCode);
        }

        var conditionDesc = offerEvent.Condition != null
            ? $"Grade: {offerEvent.Condition.Grade}, Mileage: {offerEvent.Condition.Mileage}"
            : string.Empty;

        return new SearchDocument
        {
            Id = $"Offer_{offerEvent.OfferId}",
            EntityType = EntityType.Offer,
            EntityId = offerEvent.OfferId.ToString(),
            Title = $"{offerEvent.Year} {offerEvent.Make} {offerEvent.Model}",
            Description = $"VIN: {offerEvent.Vin}. {conditionDesc}. Located in {locationString}. Price: ${offerEvent.OfferAmount:N2}",
            Keywords = keywords.Where(k => !string.IsNullOrWhiteSpace(k)).ToList(),
            SellerId = offerEvent.SellerId.ToString(),
            Vin = offerEvent.Vin,
            Make = offerEvent.Make,
            Model = offerEvent.Model,
            Year = offerEvent.Year,
            Amount = offerEvent.OfferAmount,
            Status = offerEvent.Status,
            Location = locationString,
            City = location?.City,
            State = location?.StateCode,
            Country = location?.Country,
            CreatedAt = offerEvent.CreatedAt
        };
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

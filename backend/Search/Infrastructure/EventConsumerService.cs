using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Search.Documents;
using Search.Events;
using Search.Services;

namespace Search.Infrastructure;

/// <summary>
/// Background service that consumes events from RabbitMQ and indexes them in Elasticsearch.
/// </summary>
public class EventConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public EventConsumerService(
        IServiceProvider serviceProvider,
        ILogger<EventConsumerService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for RabbitMQ to be ready
        await Task.Delay(5000, stoppingToken);

        try
        {
            await ConnectToRabbitMQ(stoppingToken);
            SetupConsumers();

            _logger.LogInformation("Event consumer started, waiting for messages...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in event consumer service");
        }
    }

    private async Task ConnectToRabbitMQ(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["MessageBroker:Host"] ?? "localhost",
            Port = int.Parse(_configuration["MessageBroker:Port"] ?? "5672"),
            UserName = _configuration["MessageBroker:Username"] ?? "admin",
            Password = _configuration["MessageBroker:Password"] ?? "password"
        };

        var maxRetries = 10;
        var retryDelay = 5000;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _logger.LogInformation("Connected to RabbitMQ");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries}): {Error}", 
                    i + 1, maxRetries, ex.Message);
                await Task.Delay(retryDelay, stoppingToken);
            }
        }

        throw new Exception("Failed to connect to RabbitMQ after maximum retries");
    }

    private void SetupConsumers()
    {
        if (_channel == null) return;

        // Define exchange and queues
        _channel.ExchangeDeclare("domain_events", ExchangeType.Topic, durable: true);

        // Offer events queue
        var offerQueue = _channel.QueueDeclare("search.offers", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("search.offers", "domain_events", "offer.*");

        // Purchase events queue
        var purchaseQueue = _channel.QueueDeclare("search.purchases", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("search.purchases", "domain_events", "purchase.*");

        // Transport events queue
        var transportQueue = _channel.QueueDeclare("search.transports", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind("search.transports", "domain_events", "transport.*");

        // Set up consumers
        var offerConsumer = new EventingBasicConsumer(_channel);
        offerConsumer.Received += async (model, ea) => await HandleOfferEvent(ea);
        _channel.BasicConsume("search.offers", autoAck: false, consumer: offerConsumer);

        var purchaseConsumer = new EventingBasicConsumer(_channel);
        purchaseConsumer.Received += async (model, ea) => await HandlePurchaseEvent(ea);
        _channel.BasicConsume("search.purchases", autoAck: false, consumer: purchaseConsumer);

        var transportConsumer = new EventingBasicConsumer(_channel);
        transportConsumer.Received += async (model, ea) => await HandleTransportEvent(ea);
        _channel.BasicConsume("search.transports", autoAck: false, consumer: transportConsumer);

        _logger.LogInformation("RabbitMQ consumers set up for offers, purchases, and transports");
    }

    private async Task HandleOfferEvent(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            _logger.LogInformation("Received offer event: {RoutingKey}", routingKey);

            using var scope = _serviceProvider.CreateScope();
            var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

            if (routingKey.EndsWith("created") || routingKey.EndsWith("updated"))
            {
                var @event = JsonSerializer.Deserialize<OfferCreatedEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (@event != null)
                {
                    var document = MapToOfferDocument(@event);
                    await elasticsearchService.IndexOfferAsync(document);
                }
            }
            else if (routingKey.EndsWith("cancelled"))
            {
                var @event = JsonSerializer.Deserialize<OfferCancelledEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (@event != null)
                {
                    // Update the document with cancelled status instead of deleting
                    var document = new OfferDocument
                    {
                        OfferId = @event.OfferId,
                        SellerId = @event.SellerId,
                        Status = @event.Status,
                        UpdatedAt = @event.UpdatedAt
                    };
                    await elasticsearchService.IndexOfferAsync(document);
                }
            }

            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling offer event");
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task HandlePurchaseEvent(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            _logger.LogInformation("Received purchase event: {RoutingKey}", routingKey);

            using var scope = _serviceProvider.CreateScope();
            var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

            if (routingKey.EndsWith("created"))
            {
                var @event = JsonSerializer.Deserialize<PurchaseCreatedEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (@event != null)
                {
                    var document = MapToPurchaseDocument(@event);
                    await elasticsearchService.IndexPurchaseAsync(document);
                }
            }
            else if (routingKey.EndsWith("updated"))
            {
                var @event = JsonSerializer.Deserialize<PurchaseUpdatedEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (@event != null)
                {
                    var document = MapToPurchaseDocument(@event);
                    await elasticsearchService.IndexPurchaseAsync(document);
                }
            }

            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling purchase event");
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task HandleTransportEvent(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            _logger.LogInformation("Received transport event: {RoutingKey}", routingKey);

            using var scope = _serviceProvider.CreateScope();
            var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

            if (routingKey.EndsWith("created"))
            {
                var @event = JsonSerializer.Deserialize<TransportCreatedEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (@event != null)
                {
                    var document = MapToTransportDocument(@event);
                    await elasticsearchService.IndexTransportAsync(document);
                }
            }
            else if (routingKey.EndsWith("updated"))
            {
                var @event = JsonSerializer.Deserialize<TransportUpdatedEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (@event != null)
                {
                    var document = MapToTransportDocument(@event);
                    await elasticsearchService.IndexTransportAsync(document);
                }
            }

            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling transport event");
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private OfferDocument MapToOfferDocument(OfferCreatedEvent @event)
    {
        return new OfferDocument
        {
            OfferId = @event.OfferId,
            SellerId = @event.SellerId,
            Vin = @event.Vin,
            Make = @event.Make,
            Model = @event.Model,
            Year = @event.Year,
            OfferAmount = @event.OfferAmount,
            City = @event.Location.City,
            State = @event.Location.State,
            ZipCode = @event.Location.ZipCode,
            Country = @event.Location.Country,
            Mileage = @event.Condition.Mileage,
            ExteriorCondition = @event.Condition.Exterior,
            InteriorCondition = @event.Condition.Interior,
            MechanicalCondition = @event.Condition.Mechanical,
            Status = @event.Status,
            CreatedAt = @event.CreatedAt
        };
    }

    private PurchaseDocument MapToPurchaseDocument(PurchaseCreatedEvent @event)
    {
        return new PurchaseDocument
        {
            PurchaseId = @event.PurchaseId,
            BuyerId = @event.BuyerId,
            OfferId = @event.OfferId,
            Amount = @event.Amount,
            Status = @event.Status,
            PurchaseDate = @event.PurchaseDate,
            CreatedAt = @event.CreatedAt
        };
    }

    private PurchaseDocument MapToPurchaseDocument(PurchaseUpdatedEvent @event)
    {
        return new PurchaseDocument
        {
            PurchaseId = @event.PurchaseId,
            BuyerId = @event.BuyerId,
            OfferId = @event.OfferId,
            Amount = @event.Amount,
            Status = @event.Status,
            PurchaseDate = @event.PurchaseDate,
            CreatedAt = @event.CreatedAt,
            UpdatedAt = @event.UpdatedAt
        };
    }

    private TransportDocument MapToTransportDocument(TransportCreatedEvent @event)
    {
        return new TransportDocument
        {
            TransportId = @event.TransportId,
            CarrierId = @event.CarrierId,
            PurchaseId = @event.PurchaseId,
            PickupStreet = @event.PickupStreet,
            PickupCity = @event.PickupCity,
            PickupState = @event.PickupStateCode,
            PickupZipCode = @event.PickupZipCode,
            PickupCountry = @event.PickupCountry,
            DeliveryStreet = @event.DeliveryStreet,
            DeliveryCity = @event.DeliveryCity,
            DeliveryState = @event.DeliveryStateCode,
            DeliveryZipCode = @event.DeliveryZipCode,
            DeliveryCountry = @event.DeliveryCountry,
            ScheduleDate = @event.ScheduleDate,
            Status = @event.Status,
            EstimatedCost = @event.EstimatedCost,
            Notes = @event.Notes,
            CreatedAt = @event.CreatedAt
        };
    }

    private TransportDocument MapToTransportDocument(TransportUpdatedEvent @event)
    {
        return new TransportDocument
        {
            TransportId = @event.TransportId,
            CarrierId = @event.CarrierId,
            PurchaseId = @event.PurchaseId,
            PickupStreet = @event.PickupStreet,
            PickupCity = @event.PickupCity,
            PickupState = @event.PickupStateCode,
            PickupZipCode = @event.PickupZipCode,
            PickupCountry = @event.PickupCountry,
            DeliveryStreet = @event.DeliveryStreet,
            DeliveryCity = @event.DeliveryCity,
            DeliveryState = @event.DeliveryStateCode,
            DeliveryZipCode = @event.DeliveryZipCode,
            DeliveryCountry = @event.DeliveryCountry,
            ScheduleDate = @event.ScheduleDate,
            Status = @event.Status,
            EstimatedCost = @event.EstimatedCost,
            ActualCost = @event.ActualCost,
            Notes = @event.Notes,
            CreatedAt = @event.CreatedAt,
            UpdatedAt = @event.UpdatedAt
        };
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

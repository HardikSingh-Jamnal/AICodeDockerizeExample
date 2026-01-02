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
/// Background service that consumes transport events from RabbitMQ and indexes them in Elasticsearch.
/// </summary>
public class TransportEventConsumer : BackgroundService
{
    private readonly ILogger<TransportEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "transports.events";
    private const string QueueName = "search.transport.events";

    public TransportEventConsumer(
        ILogger<TransportEventConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(7000, stoppingToken); // Wait for RabbitMQ to be ready

        try
        {
            InitializeRabbitMq();
            StartConsuming(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ consumer for transports");
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

        // Declare exchange
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

        // Bind to all transport events
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
                _logger.LogInformation("Received transport event: {EventType}", eventType);

                await ProcessTransportEventAsync(message, eventType, stoppingToken);

                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transport event: {EventType}", eventType);
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ProcessTransportEventAsync(string message, string eventType, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var transportEvent = JsonSerializer.Deserialize<TransportEvent>(message, options);

        if (transportEvent == null)
        {
            _logger.LogWarning("Failed to deserialize transport event");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

        if (eventType.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) ||
            eventType.Contains("Deleted", StringComparison.OrdinalIgnoreCase))
        {
            var documentId = $"Transport_{transportEvent.TransportId}";
            await elasticsearchService.DeleteDocumentAsync(documentId, cancellationToken);
            return;
        }

        var document = MapToSearchDocument(transportEvent);
        await elasticsearchService.IndexDocumentAsync(document, cancellationToken);
    }

    private SearchDocument MapToSearchDocument(TransportEvent transportEvent)
    {
        var keywords = new List<string>
        {
            transportEvent.Status,
            transportEvent.PickupCity,
            transportEvent.PickupStateCode,
            transportEvent.DeliveryCity,
            transportEvent.DeliveryStateCode,
            $"Purchase-{transportEvent.PurchaseId}"
        };

        var route = $"{transportEvent.PickupCity}, {transportEvent.PickupStateCode} → {transportEvent.DeliveryCity}, {transportEvent.DeliveryStateCode}";

        return new SearchDocument
        {
            Id = $"Transport_{transportEvent.TransportId}",
            EntityType = EntityType.Transport,
            EntityId = transportEvent.TransportId.ToString(),
            Title = $"Transport #{transportEvent.TransportId}: {route}",
            Description = $"Transport from {transportEvent.PickupCity} to {transportEvent.DeliveryCity}. Scheduled: {transportEvent.ScheduleDate:yyyy-MM-dd}. Status: {transportEvent.Status}. {transportEvent.Notes}",
            Keywords = keywords.Where(k => !string.IsNullOrWhiteSpace(k)).ToList(),
            CarrierId = transportEvent.CarrierId.ToString(),
            Amount = transportEvent.EstimatedCost,
            Status = transportEvent.Status,
            Location = route,
            City = transportEvent.DeliveryCity,
            State = transportEvent.DeliveryStateCode,
            Country = transportEvent.DeliveryCountry,
            CreatedAt = transportEvent.CreatedAt
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

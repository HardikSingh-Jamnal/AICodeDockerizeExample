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
/// Background service that consumes purchase events from RabbitMQ and indexes them in Elasticsearch.
/// </summary>
public class PurchaseEventConsumer : BackgroundService
{
    private readonly ILogger<PurchaseEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "purchases.events";
    private const string QueueName = "search.purchase.events";

    public PurchaseEventConsumer(
        ILogger<PurchaseEventConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(6000, stoppingToken); // Wait for RabbitMQ to be ready

        try
        {
            InitializeRabbitMq();
            StartConsuming(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ consumer for purchases");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async void InitializeRabbitMq()
    {
        var host = _configuration["MessageBroker:Host"] ?? "localhost";
        var username = _configuration["MessageBroker:Username"];
        var password = _configuration["MessageBroker:Password"];
        var port = _configuration["MessageBroker:Port"] ?? "5672";

        // Validate required configuration
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogError("RabbitMQ credentials not configured. Check MessageBroker:Username and MessageBroker:Password settings");
            throw new InvalidOperationException("RabbitMQ credentials are required");
        }

        _logger.LogInformation("Attempting to connect to RabbitMQ at {Host}:{Port} with user {Username}",
            host, port, username);

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = username,
            Password = password,
            Port = int.Parse(port),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            ContinuationTimeout = TimeSpan.FromSeconds(10)
        };

        // Retry connection with exponential backoff
        var maxRetries = 5;
        var delay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
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

                // Bind to all purchase events
                _channel.QueueBind(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: "#");

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

                _logger.LogInformation("RabbitMQ consumer initialized successfully for {Queue}", QueueName);
                return;
            }
            catch (RabbitMQ.Client.Exceptions.AuthenticationFailureException ex)
            {
                _logger.LogError(ex, "RabbitMQ authentication failed on attempt {Attempt}. Check credentials for user {Username}",
                    attempt, username);

                if (attempt == maxRetries)
                    throw;
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                _logger.LogWarning(ex, "Cannot reach RabbitMQ broker at {Host}:{Port} (attempt {Attempt}/{MaxRetries})",
                    host, port, attempt, maxRetries);

                if (attempt == maxRetries)
                    throw;
            }

            await Task.Delay(delay);
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
        }
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
                _logger.LogInformation("Received purchase event: {EventType}", eventType);

                await ProcessPurchaseEventAsync(message, eventType, stoppingToken);

                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing purchase event: {EventType}", eventType);
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);
    }

    private async Task ProcessPurchaseEventAsync(string message, string eventType, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var purchaseEvent = JsonSerializer.Deserialize<PurchaseEvent>(message, options);

        if (purchaseEvent == null)
        {
            _logger.LogWarning("Failed to deserialize purchase event");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();

        if (eventType.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) ||
            eventType.Contains("Deleted", StringComparison.OrdinalIgnoreCase))
        {
            var documentId = $"Purchase_{purchaseEvent.PurchaseId}";
            await elasticsearchService.DeleteDocumentAsync(documentId, cancellationToken);
            return;
        }

        var document = MapToSearchDocument(purchaseEvent);
        await elasticsearchService.IndexDocumentAsync(document, cancellationToken);
    }

    private SearchDocument MapToSearchDocument(PurchaseEvent purchaseEvent)
    {
        var keywords = new List<string>
        {
            purchaseEvent.Status,
            $"Offer-{purchaseEvent.OfferId}"
        };

        return new SearchDocument
        {
            Id = $"Purchase_{purchaseEvent.PurchaseId}",
            EntityType = EntityType.Purchase,
            EntityId = purchaseEvent.PurchaseId.ToString(),
            Title = $"Purchase #{purchaseEvent.PurchaseId}",
            Description = $"Purchase of Offer #{purchaseEvent.OfferId}. Amount: ${purchaseEvent.Amount:N2}. Status: {purchaseEvent.Status}",
            Keywords = keywords.Where(k => !string.IsNullOrWhiteSpace(k)).ToList(),
            BuyerId = purchaseEvent.BuyerId.ToString(),
            Amount = purchaseEvent.Amount,
            Status = purchaseEvent.Status,
            CreatedAt = purchaseEvent.CreatedAt
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

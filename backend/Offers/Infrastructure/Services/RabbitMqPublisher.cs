using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Offers.Infrastructure.Services;

/// <summary>
/// RabbitMQ implementation of the message publisher.
/// Publishes messages to the domain_events exchange.
/// </summary>
public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly ConnectionFactory _factory;
    private readonly object _connectionLock = new object();
    private IConnection? _connection;
    private IModel? _channel;
    private const string ExchangeName = "domain_events";

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        _factory = new ConnectionFactory
        {
            HostName = configuration["MessageBroker:Host"] ?? "localhost",
            UserName = configuration["MessageBroker:Username"] ?? "admin",
            Password = configuration["MessageBroker:Password"] ?? "password",
            Port = int.Parse(configuration["MessageBroker:Port"] ?? "5672"),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _logger.LogInformation("RabbitMQ publisher configured for host: {Host}:{Port}", _factory.HostName, _factory.Port);
    }

    private async Task<IModel> EnsureChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_channel != null && _channel.IsOpen)
        {
            return _channel;
        }

        lock (_connectionLock)
        {
            if (_channel != null && _channel.IsOpen)
            {
                return _channel;
            }

            // Dispose old connections
            _channel?.Dispose();
            _connection?.Dispose();

            var retryCount = 0;
            var maxRetries = 5;
            var baseDelay = TimeSpan.FromSeconds(2);

            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation("Attempting to connect to RabbitMQ... (attempt {Attempt}/{MaxRetries})", 
                        retryCount + 1, maxRetries);
                    
                    _connection = _factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    // Declare the exchange
                    _channel.ExchangeDeclare(
                        exchange: ExchangeName,
                        type: ExchangeType.Topic,
                        durable: true,
                        autoDelete: false);

                    _logger.LogInformation("RabbitMQ publisher successfully connected to exchange: {Exchange}", ExchangeName);
                    return _channel;
                }
                catch (BrokerUnreachableException ex)
                {
                    retryCount++;
                    _logger.LogWarning("Failed to connect to RabbitMQ (attempt {Attempt}/{MaxRetries}): {Error}", 
                        retryCount, maxRetries, ex.Message);
                    
                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to connect to RabbitMQ after {MaxRetries} attempts", maxRetries);
                        throw;
                    }

                    var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, retryCount - 1));
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error connecting to RabbitMQ");
                    throw;
                }
            }

            throw new InvalidOperationException("Unable to establish RabbitMQ connection");
        }
    }

    public async Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await EnsureChannelAsync(cancellationToken);
            
            var body = Encoding.UTF8.GetBytes(payload);
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = eventType;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Use routing key format: entity.action (e.g., "offer.created", "offer.updated")
            var routingKey = eventType.ToLower() switch
            {
                "offercreated" => "offer.created",
                "offerupdated" => "offer.updated",
                "offercancelled" => "offer.cancelled",
                _ => $"offer.{eventType.ToLower()}"
            };

            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published {EventType} event to RabbitMQ with routing key {RoutingKey}", eventType, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event to RabbitMQ", eventType);
            throw;
        }
    }

    public void Dispose()
    {
        lock (_connectionLock)
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}

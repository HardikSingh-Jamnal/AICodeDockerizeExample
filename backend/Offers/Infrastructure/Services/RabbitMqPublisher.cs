using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Offers.Infrastructure.Services;

/// <summary>
/// RabbitMQ implementation of the message publisher.
/// Publishes messages to the domain_events exchange.
/// </summary>
public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "domain_events";

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = configuration["MessageBroker:Host"] ?? "localhost",
            UserName = configuration["MessageBroker:Username"] ?? "admin",
            Password = configuration["MessageBroker:Password"] ?? "password",
            Port = int.Parse(configuration["MessageBroker:Port"] ?? "5672"),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare the exchange
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _logger.LogInformation("RabbitMQ publisher initialized for exchange: {Exchange}", ExchangeName);
    }

    public Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = Encoding.UTF8.GetBytes(payload);
            var properties = _channel.CreateBasicProperties();
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

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published {EventType} event to RabbitMQ with routing key {RoutingKey}", eventType, routingKey);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} event to RabbitMQ", eventType);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}

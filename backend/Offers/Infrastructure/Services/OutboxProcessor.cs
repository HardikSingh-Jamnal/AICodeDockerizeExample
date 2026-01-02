using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Offers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client.Exceptions;

namespace Offers.Infrastructure.Services;

/// <summary>
/// Background service that processes the outbox table and publishes messages to RabbitMQ.
/// Implements the Transactional Outbox pattern for at-least-once delivery.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor starting - waiting for system initialization...");
        
        // Wait for a short period before starting to allow dependencies to initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        
        _logger.LogInformation("Outbox Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OffersDbContext>();
        
        try
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

            // Get unprocessed messages
            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .OrderBy(m => m.CreatedAt)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);

            if (!messages.Any())
                return;

            _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

            foreach (var message in messages)
            {
                try
                {
                    // Publish to RabbitMQ
                    await publisher.PublishAsync(message.EventType, message.Payload, cancellationToken);

                    // Mark as processed
                    message.ProcessedAt = DateTime.UtcNow;
                    
                    _logger.LogDebug("Successfully processed outbox message {Id} of type {EventType}", 
                        message.Id, message.EventType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process outbox message {Id}, retry count: {RetryCount}", 
                        message.Id, message.RetryCount);

                    // Increment retry count and record error
                    message.RetryCount++;
                    message.LastError = ex.Message;
                }
            }

            // Save all changes (both processed and failed messages)
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (BrokerUnreachableException ex)
        {
            _logger.LogWarning("RabbitMQ is currently unreachable, will retry in next cycle: {Error}", ex.Message);
            // Don't process messages if RabbitMQ is not available, just wait for next cycle
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing outbox messages");
            throw;
        }
    }
}

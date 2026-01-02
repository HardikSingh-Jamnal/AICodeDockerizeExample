using Microsoft.EntityFrameworkCore;
using Offers.Domain.Entities;
using Offers.Infrastructure.Data;

namespace Offers.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for OutboxMessage operations.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly OffersDbContext _context;

    public OutboxRepository(OffersDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { id }, cancellationToken);
        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages.FindAsync(new object[] { id }, cancellationToken);
        if (message != null)
        {
            message.RetryCount++;
            message.LastError = error;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

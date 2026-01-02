using MediatR;
using Microsoft.EntityFrameworkCore;
using Transport.Data;

namespace Transport.Features.DeleteTransport;

public record DeleteTransportCommand(int Id) : IRequest<bool>;

public class DeleteTransportHandler : IRequestHandler<DeleteTransportCommand, bool>
{
    private readonly TransportDbContext _context;

    public DeleteTransportHandler(TransportDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteTransportCommand request, CancellationToken cancellationToken)
    {
        var transport = await _context.Transports
            .FirstOrDefaultAsync(t => t.TransportId == request.Id, cancellationToken);

        if (transport == null)
            return false;

        _context.Transports.Remove(transport);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
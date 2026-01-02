using MediatR;
using Microsoft.EntityFrameworkCore;
using Transport.Data;
using Transport.Entities;

namespace Transport.Features.GetTransports;

public record GetTransportsQuery() : IRequest<List<TransportEntity>>;

public class GetTransportsHandler : IRequestHandler<GetTransportsQuery, List<TransportEntity>>
{
    private readonly TransportDbContext _context;

    public GetTransportsHandler(TransportDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransportEntity>> Handle(GetTransportsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Transports
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
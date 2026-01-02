using MediatR;
using Microsoft.EntityFrameworkCore;
using Transport.Data;
using Transport.Entities;

namespace Transport.Features.GetTransportById;

public record GetTransportByIdQuery(int Id) : IRequest<TransportEntity?>;

public class GetTransportByIdHandler : IRequestHandler<GetTransportByIdQuery, TransportEntity?>
{
    private readonly TransportDbContext _context;

    public GetTransportByIdHandler(TransportDbContext context)
    {
        _context = context;
    }

    public async Task<TransportEntity?> Handle(GetTransportByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Transports
            .FirstOrDefaultAsync(t => t.TransportId == request.Id, cancellationToken);
    }
}
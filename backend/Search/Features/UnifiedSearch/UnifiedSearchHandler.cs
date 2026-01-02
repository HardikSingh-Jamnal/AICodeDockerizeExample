using MediatR;
using Search.Infrastructure.Services;

namespace Search.Features.UnifiedSearch;

/// <summary>
/// Query for unified search across all entity types.
/// </summary>
public record UnifiedSearchQuery : IRequest<SearchResult>
{
    public string Query { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public string? UserId { get; init; }
    public string? UserRole { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;
}

/// <summary>
/// Handler for unified search queries.
/// </summary>
public class UnifiedSearchHandler : IRequestHandler<UnifiedSearchQuery, SearchResult>
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<UnifiedSearchHandler> _logger;

    public UnifiedSearchHandler(
        IElasticsearchService elasticsearchService,
        ILogger<UnifiedSearchHandler> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<SearchResult> Handle(UnifiedSearchQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing unified search: Query='{Query}', EntityType={EntityType}, Page={Page}", 
            request.Query, request.EntityType, request.Page);

        var searchRequest = new SearchRequest
        {
            Query = request.Query,
            EntityType = request.EntityType,
            UserId = request.UserId,
            UserRole = request.UserRole,
            Page = request.Page > 0 ? request.Page : 1,
            PageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 100) : 20,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };

        var result = await _elasticsearchService.SearchAsync(searchRequest, cancellationToken);

        _logger.LogInformation(
            "Search completed: Found {TotalCount} results, returning page {Page} of {TotalPages}", 
            result.TotalCount, result.Page, result.TotalPages);

        return result;
    }
}

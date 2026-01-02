using MediatR;
using Search.Infrastructure.Services;

namespace Search.Features.Autocomplete;

/// <summary>
/// Query for autocomplete suggestions.
/// </summary>
public record AutocompleteQuery : IRequest<IEnumerable<AutocompleteSuggestion>>
{
    public string Query { get; init; } = string.Empty;
    public int MaxSuggestions { get; init; } = 10;
}

/// <summary>
/// Handler for autocomplete queries.
/// </summary>
public class AutocompleteHandler : IRequestHandler<AutocompleteQuery, IEnumerable<AutocompleteSuggestion>>
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<AutocompleteHandler> _logger;

    public AutocompleteHandler(
        IElasticsearchService elasticsearchService,
        ILogger<AutocompleteHandler> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<IEnumerable<AutocompleteSuggestion>> Handle(AutocompleteQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing autocomplete: Query='{Query}'", request.Query);

        var maxSuggestions = request.MaxSuggestions > 0 ? Math.Min(request.MaxSuggestions, 20) : 10;

        var suggestions = await _elasticsearchService.AutocompleteAsync(
            request.Query, 
            maxSuggestions, 
            cancellationToken);

        var suggestionList = suggestions.ToList();
        _logger.LogInformation("Autocomplete returned {Count} suggestions", suggestionList.Count);

        return suggestionList;
    }
}

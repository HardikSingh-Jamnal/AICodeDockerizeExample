using Search.Domain.Entities;

namespace Search.Infrastructure.Services;

/// <summary>
/// Interface for Elasticsearch operations.
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// Ensures the search index exists with proper mappings.
    /// </summary>
    Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Index a single document.
    /// </summary>
    Task IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Index multiple documents in bulk.
    /// </summary>
    Task BulkIndexAsync(IEnumerable<SearchDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a document by ID.
    /// </summary>
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform a unified search across all entity types.
    /// </summary>
    Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get autocomplete suggestions.
    /// </summary>
    Task<IEnumerable<AutocompleteSuggestion>> AutocompleteAsync(string query, int maxSuggestions = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Search request parameters.
/// </summary>
public record SearchRequest
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
/// Search result with pagination info.
/// </summary>
public record SearchResult
{
    public IEnumerable<SearchDocument> Documents { get; init; } = new List<SearchDocument>();
    public long TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Autocomplete suggestion.
/// </summary>
public record AutocompleteSuggestion
{
    public string Text { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public double Score { get; init; }
}

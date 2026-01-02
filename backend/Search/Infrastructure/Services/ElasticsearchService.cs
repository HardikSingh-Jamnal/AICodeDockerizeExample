using Elasticsearch.Net;
using Nest;
using Search.Domain.Entities;
using Search.Domain.Enums;

namespace Search.Infrastructure.Services;

/// <summary>
/// Elasticsearch service implementation for search operations.
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        _indexName = configuration["Elasticsearch:IndexName"] ?? "marketplace_search";

        var uri = new Uri(configuration["Elasticsearch:Uri"] ?? "http://localhost:9200");
        var settings = new ConnectionSettings(uri)
            .DefaultIndex(_indexName)
            .EnableDebugMode()
            .PrettyJson()
            .RequestTimeout(TimeSpan.FromSeconds(30))
            .DefaultMappingFor<SearchDocument>(m => m.IdProperty(p => p.Id));

        _client = new ElasticClient(settings);
        _logger.LogInformation("Elasticsearch client initialized for {Uri}, index: {Index}", uri, _indexName);
    }

    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        var existsResponse = await _client.Indices.ExistsAsync(_indexName, ct: cancellationToken);

        if (existsResponse.Exists)
        {
            _logger.LogInformation("Index {IndexName} already exists", _indexName);
            return;
        }

        var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(0)
                .Analysis(a => a
                    .Analyzers(an => an
                        .Custom("autocomplete_analyzer", ca => ca
                            .Tokenizer("autocomplete_tokenizer")
                            .Filters("lowercase"))
                        .Custom("autocomplete_search_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filters("lowercase")))
                    .Tokenizers(t => t
                        .EdgeNGram("autocomplete_tokenizer", e => e
                            .MinGram(2)
                            .MaxGram(20)
                            .TokenChars(TokenChar.Letter, TokenChar.Digit)))))
            .Map<SearchDocument>(m => m
                .AutoMap()
                .Properties(p => p
                    .Keyword(k => k.Name(n => n.Id))
                    .Keyword(k => k.Name(n => n.EntityType))
                    .Keyword(k => k.Name(n => n.EntityId))
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer("standard")
                        .Fields(f => f
                            .Text(tf => tf
                                .Name("autocomplete")
                                .Analyzer("autocomplete_analyzer")
                                .SearchAnalyzer("autocomplete_search_analyzer"))
                            .Keyword(kf => kf
                                .Name("keyword")
                                .IgnoreAbove(256))))
                    .Text(t => t
                        .Name(n => n.Description)
                        .Analyzer("standard"))
                    .Keyword(k => k.Name(n => n.Keywords))
                    .Keyword(k => k.Name(n => n.SellerId))
                    .Keyword(k => k.Name(n => n.BuyerId))
                    .Keyword(k => k.Name(n => n.CarrierId))
                    .Keyword(k => k.Name(n => n.Vin))
                    .Text(t => t.Name(n => n.Make))
                    .Text(t => t.Name(n => n.Model))
                    .Number(n => n.Name(x => x.Year).Type(NumberType.Integer))
                    .Number(n => n.Name(x => x.Amount).Type(NumberType.Double))
                    .Keyword(k => k.Name(n => n.Status))
                    .Text(t => t.Name(n => n.Location))
                    .Keyword(k => k.Name(n => n.City))
                    .Keyword(k => k.Name(n => n.State))
                    .Keyword(k => k.Name(n => n.Country))
                    .Date(d => d.Name(n => n.CreatedAt))
                    .Date(d => d.Name(n => n.UpdatedAt))
                    .Date(d => d.Name(n => n.IndexedAt)))),
            cancellationToken);

        if (!createResponse.IsValid)
        {
            _logger.LogError("Failed to create index {IndexName}: {Error}", _indexName, createResponse.DebugInformation);
            throw new InvalidOperationException($"Failed to create Elasticsearch index: {createResponse.DebugInformation}");
        }

        _logger.LogInformation("Created index {IndexName}", _indexName);
    }

    public async Task IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default)
    {
        document.IndexedAt = DateTime.UtcNow;

        var response = await _client.IndexAsync(document, i => i
            .Index(_indexName)
            .Id(document.Id)
            .Refresh(Refresh.True), cancellationToken);

        if (!response.IsValid)
        {
            _logger.LogError("Failed to index document {DocumentId}: {Error}", document.Id, response.DebugInformation);
            throw new InvalidOperationException($"Failed to index document: {response.DebugInformation}");
        }

        _logger.LogInformation("Indexed document {DocumentId} of type {EntityType}", document.Id, document.EntityType);
    }

    public async Task BulkIndexAsync(IEnumerable<SearchDocument> documents, CancellationToken cancellationToken = default)
    {
        var documentList = documents.ToList();
        if (!documentList.Any()) return;

        foreach (var doc in documentList)
        {
            doc.IndexedAt = DateTime.UtcNow;
        }

        var response = await _client.BulkAsync(b => b
            .Index(_indexName)
            .IndexMany(documentList)
            .Refresh(Refresh.True), cancellationToken);

        if (response.Errors)
        {
            var errors = response.ItemsWithErrors.Select(i => i.Error?.Reason);
            _logger.LogError("Bulk index had errors: {Errors}", string.Join(", ", errors));
        }

        _logger.LogInformation("Bulk indexed {Count} documents", documentList.Count);
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync<SearchDocument>(documentId, d => d
            .Index(_indexName)
            .Refresh(Refresh.True), cancellationToken);

        if (!response.IsValid && response.Result != Result.NotFound)
        {
            _logger.LogError("Failed to delete document {DocumentId}: {Error}", documentId, response.DebugInformation);
        }

        _logger.LogInformation("Deleted document {DocumentId}", documentId);
    }

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var from = (request.Page - 1) * request.PageSize;

        var searchResponse = await _client.SearchAsync<SearchDocument>(s => s
            .Index(_indexName)
            .From(from)
            .Size(request.PageSize)
            .Query(q => BuildSearchQuery(q, request))
            .Sort(st => BuildSort(st, request))
            .Highlight(h => h
                .Fields(
                    f => f.Field(p => p.Title),
                    f => f.Field(p => p.Description),
                    f => f.Field(p => p.Keywords))),
            cancellationToken);

        if (!searchResponse.IsValid)
        {
            _logger.LogError("Search failed: {Error}", searchResponse.DebugInformation);
            return new SearchResult { Page = request.Page, PageSize = request.PageSize };
        }

        return new SearchResult
        {
            Documents = searchResponse.Documents,
            TotalCount = searchResponse.Total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<IEnumerable<AutocompleteSuggestion>> AutocompleteAsync(string query, int maxSuggestions = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Enumerable.Empty<AutocompleteSuggestion>();
        }

        var response = await _client.SearchAsync<SearchDocument>(s => s
            .Index(_indexName)
            .Size(maxSuggestions)
            .Source(src => src.Includes(i => i
                .Field(f => f.Title)
                .Field(f => f.EntityType)
                .Field(f => f.EntityId)))
            .Query(q => q
                .Bool(b => b
                    .Should(
                        sh => sh.Match(m => m
                            .Field("title.autocomplete")
                            .Query(query)
                            .Fuzziness(Fuzziness.Auto)),
                        sh => sh.Prefix(p => p
                            .Field(f => f.Vin)
                            .Value(query.ToUpperInvariant())),
                        sh => sh.Match(m => m
                            .Field(f => f.Keywords)
                            .Query(query)))
                    .MinimumShouldMatch(1))),
            cancellationToken);

        if (!response.IsValid)
        {
            _logger.LogError("Autocomplete failed: {Error}", response.DebugInformation);
            return Enumerable.Empty<AutocompleteSuggestion>();
        }

        return response.Hits.Select(h => new AutocompleteSuggestion
        {
            Text = h.Source.Title,
            EntityType = h.Source.EntityType.ToString(),
            EntityId = h.Source.EntityId,
            Score = h.Score ?? 0
        });
    }

    private QueryContainer BuildSearchQuery(QueryContainerDescriptor<SearchDocument> q, SearchRequest request)
    {
        var queries = new List<Func<QueryContainerDescriptor<SearchDocument>, QueryContainer>>();

        // Main search query with fuzzy matching
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            queries.Add(mq => mq.Bool(b => b
                .Should(
                    sh => sh.MultiMatch(mm => mm
                        .Fields(f => f
                            .Field(p => p.Title, boost: 3)
                            .Field(p => p.Description)
                            .Field(p => p.Make, boost: 2)
                            .Field(p => p.Model, boost: 2)
                            .Field(p => p.Keywords, boost: 2))
                        .Query(request.Query)
                        .Fuzziness(Fuzziness.Auto)
                        .Type(TextQueryType.BestFields)),
                    sh => sh.Term(t => t
                        .Field(f => f.Vin)
                        .Value(request.Query.ToUpperInvariant())
                        .Boost(5)))
                .MinimumShouldMatch(1)));
        }

        // Entity type filter
        if (!string.IsNullOrWhiteSpace(request.EntityType) && 
            Enum.TryParse<EntityType>(request.EntityType, true, out var entityType))
        {
            queries.Add(fq => fq.Term(t => t.Field(f => f.EntityType).Value(entityType)));
        }

        // Role-based access control
        if (!string.IsNullOrWhiteSpace(request.UserId) && !string.IsNullOrWhiteSpace(request.UserRole))
        {
            queries.Add(BuildAccessControlQuery(request.UserId, request.UserRole));
        }

        if (!queries.Any())
        {
            return q.MatchAll();
        }

        return q.Bool(b => b.Must(queries.ToArray()));
    }

    private Func<QueryContainerDescriptor<SearchDocument>, QueryContainer> BuildAccessControlQuery(string userId, string userRole)
    {
        return acq =>
        {
            switch (userRole.ToLowerInvariant())
            {
                case "seller":
                    // Sellers can only see their own offers
                    return acq.Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field(f => f.EntityType).Value(EntityType.Offer)),
                            m => m.Term(t => t.Field(f => f.SellerId).Value(userId))));

                case "buyer":
                    // Buyers can see all active offers + their own purchases
                    return acq.Bool(b => b
                        .Should(
                            sh => sh.Bool(bb => bb
                                .Must(
                                    m => m.Term(t => t.Field(f => f.EntityType).Value(EntityType.Offer)),
                                    m => m.Term(t => t.Field(f => f.Status).Value("Active")))),
                            sh => sh.Bool(bb => bb
                                .Must(
                                    m => m.Term(t => t.Field(f => f.EntityType).Value(EntityType.Purchase)),
                                    m => m.Term(t => t.Field(f => f.BuyerId).Value(userId)))))
                        .MinimumShouldMatch(1));

                case "carrier":
                    // Carriers can see their assigned transports
                    return acq.Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field(f => f.EntityType).Value(EntityType.Transport)),
                            m => m.Term(t => t.Field(f => f.CarrierId).Value(userId))));

                case "agent":
                    // Agents can see everything
                    return acq.MatchAll();

                default:
                    // Default: no access
                    return acq.Bool(b => b.MustNot(mn => mn.MatchAll()));
            }
        };
    }

    private IPromise<IList<ISort>> BuildSort(SortDescriptor<SearchDocument> st, SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SortBy))
        {
            return st.Descending(SortSpecialField.Score).Descending(d => d.CreatedAt);
        }

        var sortField = request.SortBy.ToLowerInvariant() switch
        {
            "createdat" => st.Field(f => f.CreatedAt, request.SortDescending ? SortOrder.Descending : SortOrder.Ascending),
            "amount" => st.Field(f => f.Amount, request.SortDescending ? SortOrder.Descending : SortOrder.Ascending),
            "year" => st.Field(f => f.Year, request.SortDescending ? SortOrder.Descending : SortOrder.Ascending),
            _ => st.Descending(SortSpecialField.Score)
        };

        return sortField;
    }
}

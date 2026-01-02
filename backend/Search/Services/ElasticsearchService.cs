using Nest;
using Search.Documents;

namespace Search.Services;

/// <summary>
/// Service for interacting with Elasticsearch.
/// Handles indexing, searching, and autocomplete operations.
/// </summary>
public interface IElasticsearchService
{
    Task CreateIndexIfNotExistsAsync();
    Task IndexOfferAsync(OfferDocument document);
    Task IndexPurchaseAsync(PurchaseDocument document);
    Task IndexTransportAsync(TransportDocument document);
    Task DeleteDocumentAsync(string id);
    Task<SearchResponse> SearchAsync(SearchRequest request);
    Task<List<string>> AutocompleteAsync(string query, string? userType, int? accountId);
}

/// <summary>
/// Search request model with role-based access control parameters.
/// </summary>
public record SearchRequest
{
    public string Query { get; init; } = string.Empty;
    public string UserType { get; init; } = string.Empty; // Seller, Buyer, Carrier, Agent
    public int? AccountId { get; init; }
    public Guid? AccountGuid { get; init; } // For Seller (uses Guid)
    public string? EntityType { get; init; } // Filter: Offer, Purchase, Transport
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Search response model with results and metadata.
/// </summary>
public record SearchResponse
{
    public List<SearchResult> Results { get; init; } = new();
    public long TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Individual search result with entity data and highlights.
/// </summary>
public record SearchResult
{
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public double Score { get; init; }
    public object Data { get; init; } = new();
    public Dictionary<string, List<string>> Highlights { get; init; } = new();
}

/// <summary>
/// Implementation of Elasticsearch service.
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;

    public ElasticsearchService(IElasticClient client, IConfiguration config, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
        _indexName = config["Elasticsearch:IndexName"] ?? "search_entities";
    }

    public async Task CreateIndexIfNotExistsAsync()
    {
        var existsResponse = await _client.Indices.ExistsAsync(_indexName);
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
                            .Filters("lowercase")
                        )
                        .Custom("autocomplete_search_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filters("lowercase")
                        )
                    )
                    .Tokenizers(t => t
                        .EdgeNGram("autocomplete_tokenizer", e => e
                            .MinGram(2)
                            .MaxGram(20)
                            .TokenChars(TokenChar.Letter, TokenChar.Digit)
                        )
                    )
                )
            )
            .Map<BaseDocument>(m => m
                .AutoMap<OfferDocument>()
                .AutoMap<PurchaseDocument>()
                .AutoMap<TransportDocument>()
                .Properties(p => p
                    .Keyword(k => k.Name(n => n.Id))
                    .Keyword(k => k.Name("entityType"))
                    .Text(t => t
                        .Name(n => n.SearchableText)
                        .Analyzer("autocomplete_analyzer")
                        .SearchAnalyzer("autocomplete_search_analyzer")
                    )
                    .Keyword(k => k.Name(n => n.Status))
                    .Date(d => d.Name(n => n.CreatedAt))
                    .Date(d => d.Name(n => n.UpdatedAt))
                    // Offer specific
                    .Keyword(k => k.Name("offerId"))
                    .Keyword(k => k.Name("sellerId"))
                    .Text(t => t.Name("vin").Analyzer("standard"))
                    .Text(t => t.Name("make").Analyzer("standard"))
                    .Text(t => t.Name("model").Analyzer("standard"))
                    .Number(n => n.Name("year").Type(NumberType.Integer))
                    .Number(n => n.Name("offerAmount").Type(NumberType.Double))
                    .Text(t => t.Name("city").Analyzer("standard"))
                    .Text(t => t.Name("state").Analyzer("standard"))
                    .Keyword(k => k.Name("zipCode"))
                    .Number(n => n.Name("mileage").Type(NumberType.Integer))
                    // Purchase specific
                    .Number(n => n.Name("purchaseId").Type(NumberType.Integer))
                    .Number(n => n.Name("buyerId").Type(NumberType.Integer))
                    .Number(n => n.Name("amount").Type(NumberType.Double))
                    .Date(d => d.Name("purchaseDate"))
                    // Transport specific
                    .Number(n => n.Name("transportId").Type(NumberType.Integer))
                    .Number(n => n.Name("carrierId").Type(NumberType.Integer))
                    .Text(t => t.Name("pickupCity").Analyzer("standard"))
                    .Text(t => t.Name("pickupState").Analyzer("standard"))
                    .Text(t => t.Name("deliveryCity").Analyzer("standard"))
                    .Text(t => t.Name("deliveryState").Analyzer("standard"))
                    .Date(d => d.Name("scheduleDate"))
                )
            )
        );

        if (!createResponse.IsValid)
        {
            _logger.LogError("Failed to create index: {Error}", createResponse.DebugInformation);
            throw new Exception($"Failed to create index: {createResponse.DebugInformation}");
        }

        _logger.LogInformation("Created index {IndexName}", _indexName);
    }

    public async Task IndexOfferAsync(OfferDocument document)
    {
        document.Id = $"offer_{document.OfferId}";
        document.BuildSearchableText();

        var response = await _client.IndexAsync(document, i => i.Index(_indexName).Id(document.Id));
        if (!response.IsValid)
        {
            _logger.LogError("Failed to index offer {OfferId}: {Error}", document.OfferId, response.DebugInformation);
        }
        else
        {
            _logger.LogInformation("Indexed offer {OfferId}", document.OfferId);
        }
    }

    public async Task IndexPurchaseAsync(PurchaseDocument document)
    {
        document.Id = $"purchase_{document.PurchaseId}";
        document.BuildSearchableText();

        var response = await _client.IndexAsync(document, i => i.Index(_indexName).Id(document.Id));
        if (!response.IsValid)
        {
            _logger.LogError("Failed to index purchase {PurchaseId}: {Error}", document.PurchaseId, response.DebugInformation);
        }
        else
        {
            _logger.LogInformation("Indexed purchase {PurchaseId}", document.PurchaseId);
        }
    }

    public async Task IndexTransportAsync(TransportDocument document)
    {
        document.Id = $"transport_{document.TransportId}";
        document.BuildSearchableText();

        var response = await _client.IndexAsync(document, i => i.Index(_indexName).Id(document.Id));
        if (!response.IsValid)
        {
            _logger.LogError("Failed to index transport {TransportId}: {Error}", document.TransportId, response.DebugInformation);
        }
        else
        {
            _logger.LogInformation("Indexed transport {TransportId}", document.TransportId);
        }
    }

    public async Task DeleteDocumentAsync(string id)
    {
        var response = await _client.DeleteAsync<BaseDocument>(id, d => d.Index(_indexName));
        if (!response.IsValid)
        {
            _logger.LogError("Failed to delete document {Id}: {Error}", id, response.DebugInformation);
        }
        else
        {
            _logger.LogInformation("Deleted document {Id}", id);
        }
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        var from = (request.Page - 1) * request.PageSize;

        var searchDescriptor = new SearchDescriptor<BaseDocument>()
            .Index(_indexName)
            .From(from)
            .Size(request.PageSize)
            .Query(q => BuildQuery(q, request))
            .Highlight(h => h
                .Fields(
                    f => f.Field("searchableText"),
                    f => f.Field("vin"),
                    f => f.Field("make"),
                    f => f.Field("model"),
                    f => f.Field("city"),
                    f => f.Field("pickupCity"),
                    f => f.Field("deliveryCity")
                )
                .PreTags("<em>")
                .PostTags("</em>")
            )
            .Sort(s => s
                .Descending(SortSpecialField.Score)
                .Descending(d => d.CreatedAt)
            );

        var response = await _client.SearchAsync<BaseDocument>(searchDescriptor);

        if (!response.IsValid)
        {
            _logger.LogError("Search failed: {Error}", response.DebugInformation);
            return new SearchResponse { Results = new List<SearchResult>(), TotalCount = 0, Page = request.Page, PageSize = request.PageSize };
        }

        var results = response.Hits.Select(hit => new SearchResult
        {
            EntityType = hit.Source.EntityType,
            EntityId = hit.Source.Id,
            Score = hit.Score ?? 0,
            Data = hit.Source,
            Highlights = hit.Highlight?.ToDictionary(h => h.Key, h => h.Value.ToList()) ?? new Dictionary<string, List<string>>()
        }).ToList();

        return new SearchResponse
        {
            Results = results,
            TotalCount = response.Total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<string>> AutocompleteAsync(string query, string? userType, int? accountId)
    {
        var response = await _client.SearchAsync<BaseDocument>(s => s
            .Index(_indexName)
            .Size(10)
            .Query(q => q
                .MultiMatch(mm => mm
                    .Query(query)
                    .Fields(f => f
                        .Field("searchableText")
                        .Field("vin")
                        .Field("make")
                        .Field("model")
                        .Field("city")
                        .Field("pickupCity")
                        .Field("deliveryCity")
                    )
                    .Type(TextQueryType.BoolPrefix)
                    .Fuzziness(Fuzziness.Auto)
                )
            )
            .Source(src => src.Includes(i => i.Field("searchableText")))
        );

        if (!response.IsValid)
        {
            _logger.LogError("Autocomplete failed: {Error}", response.DebugInformation);
            return new List<string>();
        }

        return response.Documents
            .Select(d => d.SearchableText)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .Take(10)
            .ToList();
    }

    private QueryContainer BuildQuery(QueryContainerDescriptor<BaseDocument> q, SearchRequest request)
    {
        var mustQueries = new List<Func<QueryContainerDescriptor<BaseDocument>, QueryContainer>>();

        // Text search with fuzziness for typo tolerance
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            mustQueries.Add(mq => mq
                .MultiMatch(mm => mm
                    .Query(request.Query)
                    .Fields(f => f
                        .Field("searchableText", 1.0)
                        .Field("vin", 3.0)
                        .Field("make", 2.0)
                        .Field("model", 2.0)
                        .Field("city", 1.5)
                        .Field("pickupCity", 1.5)
                        .Field("deliveryCity", 1.5)
                    )
                    .Type(TextQueryType.BestFields)
                    .Fuzziness(Fuzziness.Auto)
                    .Operator(Operator.Or)
                )
            );
        }

        // Entity type filter
        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            mustQueries.Add(mq => mq.Term(t => t.Field("entityType").Value(request.EntityType)));
        }

        // Role-based access control filters
        var filterQueries = BuildRoleBasedFilters(request);

        return q.Bool(b => b
            .Must(mustQueries.ToArray())
            .Filter(filterQueries.ToArray())
        );
    }

    private List<Func<QueryContainerDescriptor<BaseDocument>, QueryContainer>> BuildRoleBasedFilters(SearchRequest request)
    {
        var filters = new List<Func<QueryContainerDescriptor<BaseDocument>, QueryContainer>>();

        switch (request.UserType?.ToLower())
        {
            case "seller":
                // Sellers can only see their own offers
                if (request.AccountGuid.HasValue)
                {
                    filters.Add(f => f.Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field("entityType").Value("Offer")),
                            m => m.Term(t => t.Field("sellerId").Value(request.AccountGuid.Value.ToString()))
                        )
                    ));
                }
                break;

            case "buyer":
                // Buyers can see active offers OR their own purchases
                if (request.AccountId.HasValue)
                {
                    filters.Add(f => f.Bool(b => b
                        .Should(
                            s => s.Bool(bb => bb.Must(
                                m => m.Term(t => t.Field("entityType").Value("Offer")),
                                m => m.Term(t => t.Field("status").Value("Active"))
                            )),
                            s => s.Bool(bb => bb.Must(
                                m => m.Term(t => t.Field("entityType").Value("Purchase")),
                                m => m.Term(t => t.Field("buyerId").Value(request.AccountId.Value))
                            ))
                        )
                        .MinimumShouldMatch(1)
                    ));
                }
                break;

            case "carrier":
                // Carriers can see their transport assignments
                if (request.AccountId.HasValue)
                {
                    filters.Add(f => f.Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field("entityType").Value("Transport")),
                            m => m.Term(t => t.Field("carrierId").Value(request.AccountId.Value))
                        )
                    ));
                }
                break;

            case "agent":
                // Agents can see all data - no filters
                break;

            default:
                // Unknown user type - return nothing
                filters.Add(f => f.Term(t => t.Field("entityType").Value("__none__")));
                break;
        }

        return filters;
    }
}

using Nest;
using Search.Documents;
using Newtonsoft.Json.Linq;

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
                .AutoMap()
                .Properties(p => p
                    .Keyword(k => k.Name(n => n.Id))
                    .Keyword(k => k.Name(n => n.EntityType))
                    .Text(t => t
                        .Name(n => n.SearchableText)
                        .Analyzer("autocomplete_analyzer")
                        .SearchAnalyzer("autocomplete_search_analyzer")
                    )
                    .Keyword(k => k.Name(n => n.Status))
                    .Date(d => d.Name(n => n.CreatedAt))
                    .Date(d => d.Name(n => n.UpdatedAt))
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
        var response = await _client.DeleteAsync<object>(id, d => d.Index(_indexName));
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

        // Use low-level client to avoid NEST's type serialization issues
        var searchRequest = new
        {
            from,
            size = request.PageSize,
            query = BuildQueryObject(request),
            highlight = new
            {
                pre_tags = new[] { "<em>" },
                post_tags = new[] { "</em>" },
                fields = new
                {
                    searchableText = new { },
                    vin = new { },
                    make = new { },
                    model = new { },
                    city = new { },
                    pickupCity = new { },
                    deliveryCity = new { }
                }
            },
            sort = new object[]
            {
                new { _score = new { order = "desc" } },
                new { createdAt = new { order = "desc" } }
            }
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(searchRequest);
        var response = await _client.LowLevel.SearchAsync<Elasticsearch.Net.StringResponse>(
            _indexName,
            json
        );

        if (!response.Success)
        {
            _logger.LogError("Search failed: {Error}", response.OriginalException?.Message ?? "Unknown error");
            return new SearchResponse { Results = new List<SearchResult>(), TotalCount = 0, Page = request.Page, PageSize = request.PageSize };
        }

        var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(response.Body);
        var hits = responseObject?["hits"]?["hits"] as Newtonsoft.Json.Linq.JArray ?? new Newtonsoft.Json.Linq.JArray();
        var total = responseObject?["hits"]?["total"]?["value"]?.Value<long>() ?? 0;

        var results = new List<SearchResult>();
        foreach (var hit in hits)
        {
            var source = hit["_source"] as Newtonsoft.Json.Linq.JObject;
            if (source != null)
            {
                var entityType = source["entityType"]?.ToString() ?? "Unknown";
                var id = source["id"]?.ToString() ?? "";
                var score = hit["_score"]?.Value<double>() ?? 0;

                var highlights = new Dictionary<string, List<string>>();
                var highlightObj = hit["highlight"] as Newtonsoft.Json.Linq.JObject;
                if (highlightObj != null)
                {
                    foreach (var prop in highlightObj.Properties())
                    {
                        var values = prop.Value as Newtonsoft.Json.Linq.JArray;
                        if (values != null)
                        {
                            highlights[prop.Name] = values.Select(v => v.ToString()).ToList();
                        }
                    }
                }

                results.Add(new SearchResult
                {
                    EntityType = entityType,
                    EntityId = id,
                    Score = score,
                    Data = ConvertJObjectToBaseDocument(source),
                    Highlights = highlights
                });
            }
        }

        return new SearchResponse
        {
            Results = results,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private BaseDocument ConvertJObjectToBaseDocument(Newtonsoft.Json.Linq.JObject jObject)
    {
        var entityType = jObject["entityType"]?.ToString();
        
        try
        {
            return entityType switch
            {
                "Offer" => jObject.ToObject<OfferDocument>() ?? new BaseDocument(),
                "Purchase" => jObject.ToObject<PurchaseDocument>() ?? new BaseDocument(),
                "Transport" => jObject.ToObject<TransportDocument>() ?? new BaseDocument(),
                _ => new BaseDocument
                {
                    Id = jObject["id"]?.ToString() ?? "",
                    EntityType = entityType ?? "Unknown",
                    SearchableText = jObject["searchableText"]?.ToString() ?? "",
                    Status = jObject["status"]?.ToString() ?? "",
                    CreatedAt = jObject["createdAt"]?.ToObject<DateTime>() ?? DateTime.MinValue,
                    UpdatedAt = jObject["updatedAt"]?.ToObject<DateTime?>()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to convert JObject to specific document type: {Error}", ex.Message);
            return new BaseDocument
            {
                Id = jObject["id"]?.ToString() ?? "",
                EntityType = entityType ?? "Unknown",
                SearchableText = jObject["searchableText"]?.ToString() ?? "",
                Status = jObject["status"]?.ToString() ?? "",
                CreatedAt = jObject["createdAt"]?.ToObject<DateTime>() ?? DateTime.MinValue,
                UpdatedAt = jObject["updatedAt"]?.ToObject<DateTime?>()
            };
        }
    }

    private object BuildQueryObject(SearchRequest request)
    {
        var mustClauses = new List<object>();
        var filterClauses = new List<object>();

        // Text search with fuzziness for typo tolerance
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            mustClauses.Add(new
            {
                multi_match = new
                {
                    query = request.Query,
                    fields = new[] { "searchableText", "vin^3", "make^2", "model^2", "city^1.5", "pickupCity^1.5", "deliveryCity^1.5" },
                    type = "best_fields",
                    fuzziness = "AUTO",
                    @operator = "or"
                }
            });
        }

        // Entity type filter
        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            mustClauses.Add(new
            {
                term = new Dictionary<string, object>
                {
                    ["entityType"] = request.EntityType
                }
            });
        }

        // Role-based access control filters
        var roleFilters = BuildRoleBasedFilterObjects(request);
        filterClauses.AddRange(roleFilters);

        return new
        {
            @bool = new
            {
                must = mustClauses.Count > 0 ? mustClauses : null,
                filter = filterClauses.Count > 0 ? filterClauses : null
            }
        };
    }

    private List<object> BuildRoleBasedFilterObjects(SearchRequest request)
    {
        var filters = new List<object>();

        switch (request.UserType?.ToLower())
        {
            case "seller":
                // Sellers can only see their own offers
                if (request.AccountGuid.HasValue)
                {
                    filters.Add(new
                    {
                        @bool = new
                        {
                            must = new object[]
                            {
                                new { term = new Dictionary<string, object> { ["entityType"] = "Offer" } },
                                new { term = new Dictionary<string, object> { ["sellerId"] = request.AccountGuid.Value.ToString() } }
                            }
                        }
                    });
                }
                break;

            case "buyer":
                // Buyers can see active offers OR their own purchases
                if (request.AccountId.HasValue)
                {
                    filters.Add(new
                    {
                        @bool = new
                        {
                            should = new object[]
                            {
                                new
                                {
                                    @bool = new
                                    {
                                        must = new object[]
                                        {
                                            new { term = new Dictionary<string, object> { ["entityType"] = "Offer" } },
                                            new { term = new Dictionary<string, object> { ["status"] = "Active" } }
                                        }
                                    }
                                },
                                new
                                {
                                    @bool = new
                                    {
                                        must = new object[]
                                        {
                                            new { term = new Dictionary<string, object> { ["entityType"] = "Purchase" } },
                                            new { term = new Dictionary<string, object> { ["buyerId"] = request.AccountId.Value } }
                                        }
                                    }
                                }
                            },
                            minimum_should_match = 1
                        }
                    });
                }
                break;

            case "carrier":
                // Carriers can see their transport assignments
                if (request.AccountId.HasValue)
                {
                    filters.Add(new
                    {
                        @bool = new
                        {
                            must = new object[]
                            {
                                new { term = new Dictionary<string, object> { ["entityType"] = "Transport" } },
                                new { term = new Dictionary<string, object> { ["carrierId"] = request.AccountId.Value } }
                            }
                        }
                    });
                }
                break;

            case "agent":
                // Agents can see all data - no filters
                break;

            default:
                // Unknown user type - return nothing
                filters.Add(new { term = new Dictionary<string, object> { ["entityType"] = "__none__" } });
                break;
        }

        return filters;
    }

    public async Task<List<string>> AutocompleteAsync(string query, string? userType, int? accountId)
    {
        // Use low-level client to avoid NEST's type serialization issues
        var searchRequest = new
        {
            size = 10,
            query = new
            {
                multi_match = new
                {
                    query,
                    fields = new[] { "searchableText", "vin", "make", "model", "city", "pickupCity", "deliveryCity" },
                    type = "bool_prefix",
                    fuzziness = "AUTO"
                }
            },
            _source = new[] { "searchableText" }
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(searchRequest);
        var response = await _client.LowLevel.SearchAsync<Elasticsearch.Net.StringResponse>(
            _indexName,
            json
        );

        if (!response.Success)
        {
            _logger.LogError("Autocomplete failed: {Error}", response.OriginalException?.Message ?? "Unknown error");
            return new List<string>();
        }

        var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(response.Body);
        var hits = responseObject?["hits"]?["hits"] as Newtonsoft.Json.Linq.JArray ?? new Newtonsoft.Json.Linq.JArray();

        var results = new List<string>();
        foreach (var hit in hits)
        {
            var source = hit["_source"] as Newtonsoft.Json.Linq.JObject;
            if (source != null)
            {
                var searchableText = source["searchableText"]?.ToString();
                if (!string.IsNullOrEmpty(searchableText))
                {
                    results.Add(searchableText);
                }
            }
        }

        return results.Distinct().Take(10).ToList();
    }
}

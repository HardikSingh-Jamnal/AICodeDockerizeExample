using Microsoft.AspNetCore.Mvc;
using Search.Services;

namespace Search.Features;

/// <summary>
/// Controller for unified search operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IElasticsearchService elasticsearchService, ILogger<SearchController> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    /// <summary>
    /// Unified search endpoint with role-based access control.
    /// </summary>
    /// <param name="request">Search request with query, user type, and filters.</param>
    /// <returns>Paginated search results.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResponse>> Search([FromBody] SearchApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserType))
        {
            return BadRequest(new { Error = "UserType is required" });
        }

        var searchRequest = new SearchRequest
        {
            Query = request.Query ?? string.Empty,
            UserType = request.UserType,
            AccountId = request.AccountId,
            AccountGuid = request.AccountGuid,
            EntityType = request.EntityType,
            Page = request.Page > 0 ? request.Page : 1,
            PageSize = request.PageSize > 0 ? Math.Min(request.PageSize, 100) : 20
        };

        _logger.LogInformation("Search request: Query={Query}, UserType={UserType}, AccountId={AccountId}", 
            searchRequest.Query, searchRequest.UserType, searchRequest.AccountId ?? searchRequest.AccountGuid?.GetHashCode());

        var response = await _elasticsearchService.SearchAsync(searchRequest);
        return Ok(response);
    }

    /// <summary>
    /// Autocomplete endpoint for search suggestions.
    /// </summary>
    /// <param name="q">Query string for autocomplete.</param>
    /// <param name="userType">User type for access control.</param>
    /// <param name="accountId">Account ID for filtering.</param>
    /// <returns>List of autocomplete suggestions.</returns>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> Autocomplete(
        [FromQuery] string q,
        [FromQuery] string? userType = null,
        [FromQuery] int? accountId = null)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Ok(new List<string>());
        }

        var suggestions = await _elasticsearchService.AutocompleteAsync(q, userType, accountId);
        return Ok(suggestions);
    }

    /// <summary>
    /// Health check endpoint for the search service.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Search API" });
    }
}

/// <summary>
/// API request model for search.
/// </summary>
public record SearchApiRequest
{
    /// <summary>
    /// Search query text.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// User type: Seller, Buyer, Carrier, or Agent.
    /// </summary>
    public string UserType { get; init; } = string.Empty;

    /// <summary>
    /// Account ID for role-based filtering (BuyerId, CarrierId).
    /// </summary>
    public int? AccountId { get; init; }

    /// <summary>
    /// Account GUID for role-based filtering (SellerId).
    /// </summary>
    public Guid? AccountGuid { get; init; }

    /// <summary>
    /// Optional entity type filter: Offer, Purchase, or Transport.
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size (max 100).
    /// </summary>
    public int PageSize { get; init; } = 20;
}

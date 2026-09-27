using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using EnterpriseDocumentIntelligence.SearchService.Application;
using EnterpriseDocumentIntelligence.SearchService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.SearchService.Api.Controllers;

public sealed record SearchRequest(
    string Query,
    int TopK = 10);

public sealed record SearchResponse(
    string DocumentId,
    string Text,
    double Score,
    string Citation);

[ApiController]
[Route("api/v1/search")]
[Authorize]
public sealed class SearchController(
    SearchService searchService,
    ICurrentUser currentUser,
    ILogger<SearchController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IReadOnlyList<SearchResponse>>> Search(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.TenantId == Guid.Empty ||
            currentUser.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        try
        {
            var query = SearchQuery.Create(
                currentUser.TenantId,
                currentUser.UserId,
                request.Query,
                request.TopK);

            var results = await searchService.SearchAsync(
                query,
                cancellationToken);

            logger.LogInformation(
                "Search completed for Tenant={TenantId}, Results={Count}",
                currentUser.TenantId,
                results.Count);

            return Ok(
                results.Select(
                    result => new SearchResponse(
                        result.DocumentId,
                        result.Text,
                        result.Score,
                        result.Citation))
                .ToArray());
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }
}
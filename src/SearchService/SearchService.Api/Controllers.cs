using EnterpriseDocumentIntelligence.SearchService.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public sealed class SearchController(
    SearchApplicationService application) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IReadOnlyList<SearchResult>>> Search(
        SearchRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await application.SearchAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}

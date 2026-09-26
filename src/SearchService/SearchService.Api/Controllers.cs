using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.SearchService.Api;

[ApiController]
[Route("api/v1/search")]
[Authorize(Roles = Roles.Reader + "," + Roles.Contributor + "," + Roles.Administrator)]
public sealed class SearchController(ISearchApplication application) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(IReadOnlyList<SearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SearchResult>>> Search(SearchRequest request, CancellationToken ct) =>
        Ok(await application.SearchAsync(request, ct));
}
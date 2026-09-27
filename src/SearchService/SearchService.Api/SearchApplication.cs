using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
public sealed record SearchRequest(string Query,int TopK=10);
public sealed record SearchResult(string DocumentId,string Text,double Score,string Citation);
public sealed class SearchApplication(SearchClient client,AzureOpenAiEmbeddingClient embeddings,ICurrentUser user,ILogger<SearchApplication> log)
{
 public async Task<IReadOnlyList<SearchResult>>SearchAsync(SearchRequest request,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(request.Query))throw new ArgumentException("Query is required.");
  if(request.TopK is <1 or >50)throw new ArgumentOutOfRangeException(nameof(request.TopK));
  if(user.TenantId==Guid.Empty)throw new UnauthorizedAccessException();
  var vector=await embeddings.EmbedAsync(request.Query,ct);
  var options=new SearchOptions{Size=request.TopK,Filter=SearchFilterBuilder.Build(user.TenantId,user.UserId)};
  options.Select.Add("DocumentId");options.Select.Add("Text");options.Select.Add("Citation");
  options.VectorSearch=new(){Queries={new VectorizedQuery(vector){KNearestNeighborsCount=request.TopK,Fields={"ContentVector"}}}};
  var response=await client.SearchAsync<SearchDocument>(request.Query,options,ct);
  var results=new List<SearchResult>();
  await foreach(var r in response.Value.GetResultsAsync())results.Add(new(r.Document.GetString("DocumentId")??"",r.Document.GetString("Text")??"",r.Score??0d,r.Document.GetString("Citation")??""));
  log.LogInformation("Hybrid retrieval Tenant={TenantId} Results={Count}",user.TenantId,results.Count);
  return results;
 }

}
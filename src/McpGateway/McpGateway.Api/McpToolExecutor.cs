using System.Text;
using System.Text.Json;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;

public interface IMcpToolExecutor { Task<object> ExecuteAsync(string tool, JsonElement arguments, CancellationToken ct); }

public sealed class McpToolExecutor(IHttpClientFactory clients,IHttpContextAccessor context,ICurrentUser user,IConfiguration configuration):IMcpToolExecutor
{
 public Task<object> ExecuteAsync(string tool,JsonElement arguments,CancellationToken ct)=>tool switch
 {
  "document.search"=>SearchAsync(arguments,ct),
  "document.get"=>GetAsync(arguments,ct),
  "document.audit"=>AuditAsync(arguments,ct),
  _=>throw new InvalidOperationException($"Unsupported MCP tool '{tool}'.")
 };
 private async Task<object> SearchAsync(JsonElement args,CancellationToken ct){var query=args.TryGetProperty("query",out var q)?q.GetString():"";if(string.IsNullOrWhiteSpace(query))throw new ArgumentException("query is required.");var topK=args.TryGetProperty("topK",out var k)?Math.Clamp(k.GetInt32(),1,20):8;return await SendAsync("SearchService","/api/v1/search",HttpMethod.Post,new{query,topK},ct);}
 private async Task<object> GetAsync(JsonElement args,CancellationToken ct){if(!args.TryGetProperty("documentId",out var id)||!Guid.TryParse(id.GetString(),out var documentId))throw new ArgumentException("documentId must be a GUID.");return await SendAsync("DocumentService",$"/api/v1/documents/{documentId}",HttpMethod.Get,null,ct);}
 private async Task<object> AuditAsync(JsonElement args,CancellationToken ct)=>await SendAsync("AuditService","/api/v1/audit",HttpMethod.Get,null,ct);
 private async Task<object> SendAsync(string service,string path,HttpMethod method,object? body,CancellationToken ct)
 {
  var baseUrl=configuration[$"Services:{service}Url"]?.TrimEnd('/')??throw new InvalidOperationException($"Services:{service}Url missing");
  using var request=new HttpRequestMessage(method,baseUrl+path);
  var authorization=context.HttpContext?.Request.Headers.Authorization.ToString();if(!string.IsNullOrWhiteSpace(authorization))request.Headers.TryAddWithoutValidation("Authorization",authorization);
  request.Headers.TryAddWithoutValidation("X-Tenant-Id",user.TenantId.ToString());
  if(body is not null)request.Content=new StringContent(JsonSerializer.Serialize(body),Encoding.UTF8,"application/json");
  using var response=await clients.CreateClient(service).SendAsync(request,ct);var content=await response.Content.ReadAsStringAsync(ct);response.EnsureSuccessStatusCode();return JsonSerializer.Deserialize<JsonElement>(content);
 }
}
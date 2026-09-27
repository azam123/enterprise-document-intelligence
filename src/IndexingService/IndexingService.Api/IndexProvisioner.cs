using Azure;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

public sealed class IndexProvisioner(SearchIndexClient indexes, IConfiguration configuration)
{
    public async Task EnsureAsync(CancellationToken ct)
    {
        var name=configuration["AzureSearch:IndexName"]??"document-chunks";
        var dimensions=int.TryParse(configuration["AzureSearch:VectorDimensions"],out var d)?d:1536;
        var index=new SearchIndex(name)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String){IsKey=true},
                new SearchField("TenantId",SearchFieldDataType.String){IsFilterable=true},
                new SearchField("DocumentId",SearchFieldDataType.String){IsFilterable=true},
                new SearchField("ChunkId",SearchFieldDataType.String){IsFilterable=true},
                new SearchField("ChunkNumber",SearchFieldDataType.Int32){IsFilterable=true},
                new SearchField("Text",SearchFieldDataType.String){IsSearchable=true},
                new SearchField("Citation",SearchFieldDataType.String){IsSearchable=false},
                new SearchField("AllowedPrincipalIds",SearchFieldDataType.Collection(SearchFieldDataType.String)){IsFilterable=true},
                new SearchField("ContentVector",SearchFieldDataType.Collection(SearchFieldDataType.Single)){IsSearchable=true,VectorSearchDimensions=dimensions,VectorSearchProfileName="default-vector-profile"}
            },
            VectorSearch=new VectorSearch
            {
                Algorithms={new HnswAlgorithmConfiguration("default-hnsw")},
                Profiles={new VectorSearchProfile("default-vector-profile","default-hnsw")}
            }
        };
        await indexes.CreateOrUpdateIndexAsync(index,ct);
    }
}

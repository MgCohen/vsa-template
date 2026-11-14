using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.LoadDefinition;

public sealed record LoadDefinitionRequest(string? Json, string Shape) : IRequest<LoadDefinitionResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/load-definition";
}

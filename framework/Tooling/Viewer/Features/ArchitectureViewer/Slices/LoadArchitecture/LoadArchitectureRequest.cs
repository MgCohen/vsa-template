using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.LoadArchitecture;

public sealed record LoadArchitectureRequest(string Shape) : IRequest<LoadArchitectureResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/load-architecture";
}

using VSA.Contracts.Requests;

namespace ArchitectureViewer.Slices.CopyText;

public sealed record CopyTextRequest(string Text) : IRequest<CopyTextResponse>
{
    public static HttpMethod Method => HttpMethod.Post;

    public static string Route => "/api/viewer/copy-text";
}

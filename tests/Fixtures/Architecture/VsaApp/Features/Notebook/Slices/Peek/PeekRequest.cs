namespace VsaApp.Features.Notebook.Slices.Peek;

using Umpire.Vsa.Contracts.Requests;

public sealed record PeekRequest : IRequest<PeekResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/peek";
    public static Type Handler => typeof(PeekHandler);
}

namespace VsaApp.Features.Notebook.Slices.Count;

using Umpire.Vsa.Contracts.Requests;

public sealed record CountRequest : IRequest<CountResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/count";
}

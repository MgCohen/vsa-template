namespace Notes.Features.Ping.Slices.Ping;

using VSA.Contracts.Requests;

public sealed record PingRequest : IRequest<PingResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/ping";
}

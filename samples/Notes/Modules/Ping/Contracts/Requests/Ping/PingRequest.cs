using VSA.Contracts.Requests;

namespace Ping.Contracts.Requests.Ping;

public sealed record PingRequest : IRequest<PingResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/ping";
}

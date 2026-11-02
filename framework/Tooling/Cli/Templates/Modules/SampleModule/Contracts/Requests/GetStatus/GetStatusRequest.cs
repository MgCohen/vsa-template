using VSA.Contracts.Requests;

namespace SampleModule.Contracts.Requests.GetStatus;

public sealed record GetStatusRequest : IRequest<GetStatusResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/sample-module/status";
}

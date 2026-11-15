namespace SampleApp.Features.SampleModule.Slices.GetStatus;

using VSA.Contracts.Requests;

public sealed record GetStatusRequest : IRequest<GetStatusResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/sample-module/status";
}

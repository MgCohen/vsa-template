namespace SampleApp.Features.SampleModule.Slices.GetStatus;

using Umpire.Vsa.Contracts.Requests;

public sealed record GetStatusRequest : IRequest<GetStatusResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/sample-module/status";
}

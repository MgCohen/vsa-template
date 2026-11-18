namespace SampleApp.Features.SampleModule.Slices.GetStatus;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record GetStatusRequest : IRequest<GetStatusResponse> {
    public static Effect Effect => Effect.Read;
}

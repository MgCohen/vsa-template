namespace VsaApp.Features.Notebook.Slices.Ping;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record PingRequest : IRequest<PingResponse> {
    public static Effect Effect => Effect.Read;
}

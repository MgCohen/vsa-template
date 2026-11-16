namespace VsaApp.Features.Notebook.Slices.Pong;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using VsaApp.Features.Notebook.Slices.Ping;

sealed class PongHandler : RequestHandler<PingRequest, PingResponse> {
    public override Task<Result<PingResponse>> HandleAsync(Envelope<PingRequest> envelope) =>
        Task.FromResult<Result<PingResponse>>(new PingResponse());
}

using Ping.Contracts.Requests.Ping;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace Ping.Handlers.Requests;

sealed class PingHandler : RoutedHandler<PingRequest, PingResponse> {
    protected override Task<Result<PingResponse>> HandleRequestAsync(PingRequest request, CancellationToken ct) =>
        Task.FromResult<Result<PingResponse>>(new PingResponse("pong"));
}

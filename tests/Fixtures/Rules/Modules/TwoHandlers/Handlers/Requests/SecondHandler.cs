using TwoHandlers.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace TwoHandlers.Handlers.Requests;

sealed class SecondHandler : RoutedHandler<TwiceRequest, TwiceResponse> {
    protected override Task<Result<TwiceResponse>> HandleRequestAsync(TwiceRequest request, CancellationToken ct) =>
        Task.FromResult<Result<TwiceResponse>>(new TwiceResponse(request.Id));
}

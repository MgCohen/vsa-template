using Shelf.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace Shelf.Handlers.Requests;

public sealed class CountHandler : RoutedHandler<CountRequest, CountResponse> {
    protected override Task<Result<CountResponse>> HandleRequestAsync(CountRequest request, CancellationToken ct) =>
        Task.FromResult<Result<CountResponse>>(new CountResponse(0));
}

using SameRoute.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace SameRoute.Handlers.Requests;

sealed class SecondHandler : RoutedHandler<SecondRequest, SecondResponse> {
    protected override Task<Result<SecondResponse>> HandleRequestAsync(SecondRequest request, CancellationToken ct) =>
        Task.FromResult<Result<SecondResponse>>(new SecondResponse(request.Id));
}

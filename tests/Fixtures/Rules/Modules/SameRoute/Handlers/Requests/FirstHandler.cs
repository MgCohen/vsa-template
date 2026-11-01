using SameRoute.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace SameRoute.Handlers.Requests;

sealed class FirstHandler : RoutedHandler<FirstRequest, FirstResponse> {
    protected override Task<Result<FirstResponse>> HandleRequestAsync(FirstRequest request, CancellationToken ct) =>
        Task.FromResult<Result<FirstResponse>>(new FirstResponse(request.Id));
}

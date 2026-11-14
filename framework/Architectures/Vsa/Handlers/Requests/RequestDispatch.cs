using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;
using VSA.Contracts.Requests;

namespace VSA.Handlers.Requests;

static class RequestDispatch {
    internal static Task<Result<TResponse>> SendAsync<TResponse>(
        IServiceProvider services, IRequest<TResponse> request, CancellationToken ct) where TResponse : IResponse =>
        services.GetRequiredService<Pipelines>().SendAsync<TResponse>(PipelineKind.Requests, request, services, ct);
}

namespace Umpire.Vsa.Handlers.Requests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Messages.Handlers;

static class RequestDispatch {
    internal static Task<Result<TResponse>> SendAsync<TResponse>(
        IServiceProvider services, IRequest<TResponse> request, CancellationToken ct) where TResponse : IResponse =>
        services.GetRequiredService<Pipelines>().SendAsync<TResponse>(typeof(RequestHandler<,>), request, services, ct);
}

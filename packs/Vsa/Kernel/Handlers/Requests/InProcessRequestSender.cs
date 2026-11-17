namespace Umpire.Vsa.Handlers.Requests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Messages.Handlers;

sealed class InProcessRequestSender(IServiceProvider provider) : IRequestSender {
    public Task<Result<TResponse>> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse =>
        provider.GetRequiredService<Pipelines>().SendAsync<TResponse>(
            typeof(RequestHandler<,>), request ?? throw new ArgumentNullException(nameof(request)), provider, ct);
}

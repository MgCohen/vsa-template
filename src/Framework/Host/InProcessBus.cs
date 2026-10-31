using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Requests;
using VSA.Domain;
using VSA.Handlers.Requests;
using VSA.Presenters;

namespace VSA.Host;

// Sends each request to its handler in this process, in a new scope, as an HTTP request does.
sealed class InProcessBus(IServiceScopeFactory scopes) : IBus {
    static readonly ConcurrentDictionary<Type, object> dispatchers = new();

    public async Task<TResponse> Send<TResponse>(Request<TResponse> request, CancellationToken ct = default) {
        var dispatcher = (Dispatcher<TResponse>)dispatchers.GetOrAdd(request.GetType(),
            type => Activator.CreateInstance(typeof(Dispatcher<,>).MakeGenericType(type, typeof(TResponse)))!);
        await using var scope = scopes.CreateAsyncScope();
        var result = await dispatcher.HandleAsync(scope.ServiceProvider, request, ct);
        return result.IsSuccess ? result.Value : throw new RequestException(result.Error!.Code, result.Error.Message);
    }

    abstract class Dispatcher<TResponse> {
        public abstract Task<Result<TResponse>> HandleAsync(IServiceProvider services, Request<TResponse> request, CancellationToken ct);
    }

    sealed class Dispatcher<TRequest, TResponse> : Dispatcher<TResponse> where TRequest : Request<TResponse> {
        public override Task<Result<TResponse>> HandleAsync(IServiceProvider services, Request<TResponse> request, CancellationToken ct) =>
            services.GetRequiredService<RequestHandler<TRequest, TResponse>>().HandleAsync((TRequest)request, ct);
    }
}

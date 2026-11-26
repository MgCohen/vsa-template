namespace Spike.Pack;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Spike.Kernels;

static class RequestOperations {
    internal static Operation Create(Type handler) {
        var arguments = handler.GetGenericArguments();
        var send = typeof(RequestOperations)
            .GetMethod(nameof(Send), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(arguments);
        return new Operation(NameOf(arguments[0]), arguments[0],
            (services, input, ct) => (Task<OperationResult>)send.Invoke(null, [services, input, ct])!);
    }

    static async Task<OperationResult> Send<TRequest, TResponse>(IServiceProvider services, object input, CancellationToken ct)
        where TRequest : IRequest<TResponse> {
        var handler = services.GetRequiredService<RequestHandler<TRequest, TResponse>>();
        var value = await handler.Handle((TRequest)input, ct).ConfigureAwait(false);
        return new OperationResult(value, null, 0);
    }

    static string NameOf(Type request) =>
        request.Name.EndsWith("Request", StringComparison.Ordinal)
            ? request.Name.Substring(0, request.Name.Length - "Request".Length).ToLowerInvariant()
            : request.Name.ToLowerInvariant();
}

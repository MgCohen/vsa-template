namespace Umpire.Vsa.Composition;

using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public static class LocalRequests {
    public static async Task<Result<TResponse>> SendAsync<TResponse>(
        this LocalHost host, IRequest<TResponse> request, CancellationToken ct = default)
        where TResponse : IResponse =>
        ResultOf<TResponse>(await LocalHostApps.SendAsync(host, (object)request, ct).ConfigureAwait(false));

    public static Result<TResponse> Send<TResponse>(this LocalHost host, IRequest<TResponse> request)
        where TResponse : IResponse =>
        SendAsync(host, request).GetAwaiter().GetResult();

    static Result<TResponse> ResultOf<TResponse>(IMessageResult result) where TResponse : IResponse {
        if (result.Error is { } error) {
            return error;
        }
        return (TResponse)result.Value!;
    }
}

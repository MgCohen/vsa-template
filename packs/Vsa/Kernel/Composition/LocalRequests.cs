namespace Umpire.Vsa.Composition;

using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public static class LocalRequests {
    public static async Task<Result<TResponse>> SendAsync<TKernel, THost, TResponse>(
        this App<TKernel, THost> app, IRequest<TResponse> request, CancellationToken ct = default)
        where TKernel : Kernel, new()
        where THost : LocalHost, new()
        where TResponse : IResponse =>
        ResultOf<TResponse>(await LocalHostApps.SendAsync(app, request, ct).ConfigureAwait(false));

    public static Result<TResponse> Send<TKernel, THost, TResponse>(
        this App<TKernel, THost> app, IRequest<TResponse> request)
        where TKernel : Kernel, new()
        where THost : LocalHost, new()
        where TResponse : IResponse =>
        SendAsync(app, request).GetAwaiter().GetResult();

    static Result<TResponse> ResultOf<TResponse>(OperationResult result) where TResponse : IResponse {
        if (result.Error is { } error) {
            return error;
        }
        return (TResponse)result.Value!;
    }
}

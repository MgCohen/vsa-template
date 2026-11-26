namespace Umpire.Kernels;

public static class LocalHostApps {
    public static Task<OperationResult> SendAsync<TKernel>(
        this App<TKernel, LocalHost> app, object message, CancellationToken ct = default)
        where TKernel : Kernel, new() =>
        app.RunningHost is LocalHost host
            ? host.SendAsync(message, ct)
            : throw new InvalidOperationException("The app is not running. Call Start first.");

    public static OperationResult Send<TKernel>(this App<TKernel, LocalHost> app, object message)
        where TKernel : Kernel, new() =>
        app.SendAsync(message).GetAwaiter().GetResult();
}

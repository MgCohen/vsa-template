namespace Umpire.Kernels;

public static class LocalHostApps {
    public static Task<OperationResult> SendAsync<TKernel, THost>(
        this App<TKernel, THost> app, object message, CancellationToken ct = default)
        where TKernel : Kernel, new()
        where THost : LocalHost, new() =>
        app.RunningHost switch {
            LocalHost host => host.SendAsync(message, ct),
            null => throw NotRunning(app.GetType().Name),
            var other => throw new InvalidOperationException(
                $"{NameOf(app.GetType().Name)} runs on {other.GetType().Name}, and Send needs a LocalHost. "
                + "Return a LocalHost from CreateHost."),
        };

    public static OperationResult Send<TKernel, THost>(this App<TKernel, THost> app, object message)
        where TKernel : Kernel, new()
        where THost : LocalHost, new() =>
        app.SendAsync(message).GetAwaiter().GetResult();

    internal static InvalidOperationException NotRunning(string name) =>
        new($"{NameOf(name)} does not run. Call Start before Send.");

    static string NameOf(string type) => type.Split('`')[0];
}

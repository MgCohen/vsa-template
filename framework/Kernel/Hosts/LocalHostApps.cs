namespace Umpire.Kernels;

using Umpire.Messages.Contracts;

public static class LocalHostApps {
    public static Task<IMessageResult> SendAsync<TKernel, THost>(
        this App<TKernel, THost> app, object message, CancellationToken ct = default)
        where TKernel : Kernel, new()
        where THost : LocalHost, new() =>
        HostOf(app).SendAsync(message, ct);

    public static IMessageResult Send<TKernel, THost>(this App<TKernel, THost> app, object message)
        where TKernel : Kernel, new()
        where THost : LocalHost, new() =>
        app.SendAsync(message).GetAwaiter().GetResult();

    public static Task<IMessageResult> SendAsync<TKernel, THost>(
        this App<TKernel, THost> app, Operation operation, CancellationToken ct = default)
        where TKernel : Kernel, new()
        where THost : LocalHost, new() =>
        HostOf(app).SendAsync(operation, ct);

    internal static InvalidOperationException NotRunning(string name) =>
        new($"{NameOf(name)} does not run. Call Start before Send.");

    static LocalHost HostOf<TKernel, THost>(App<TKernel, THost> app)
        where TKernel : Kernel, new()
        where THost : LocalHost, new() =>
        app.RunningHost switch {
            LocalHost host => host,
            null => throw NotRunning(app.GetType().Name),
            var other => throw new InvalidOperationException(
                $"{NameOf(app.GetType().Name)} runs on {other.GetType().Name}, and Send needs a LocalHost. "
                + "Return a LocalHost from CreateHost."),
        };

    static string NameOf(string type) => type.Split('`')[0];
}

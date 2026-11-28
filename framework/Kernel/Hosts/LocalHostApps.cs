namespace Umpire.Kernels;

using Umpire.Messages.Contracts;

public static class LocalHostApps {
    public static Task<IMessageResult> SendAsync(this LocalHost host, object message, CancellationToken ct = default) =>
        host.SendMessageAsync(message, ct);

    public static IMessageResult Send(this LocalHost host, object message) =>
        host.SendMessageAsync(message, default).GetAwaiter().GetResult();

    public static Task<IMessageResult> SendAsync(this LocalHost host, Operation operation, CancellationToken ct = default) =>
        host.SendOperationAsync(operation, ct);

    internal static InvalidOperationException NotRunning(string name) =>
        new($"{name} does not run. Give it to App.Run before Send.");
}

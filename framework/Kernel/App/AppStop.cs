namespace Umpire.Kernels;

public sealed class AppStop {
    readonly object gate = new();
    Action<int>? listener;
    int? exitCode;

    public void Request(int exitCode = 0) {
        Action<int>? notify;
        lock (gate) {
            if (this.exitCode is not null) {
                return;
            }
            this.exitCode = exitCode;
            notify = listener;
        }
        notify?.Invoke(exitCode);
    }

    internal void Listen(Action<int> listener) {
        int? requested;
        lock (gate) {
            this.listener = listener;
            requested = exitCode;
        }
        if (requested is { } code) {
            listener(code);
        }
    }
}

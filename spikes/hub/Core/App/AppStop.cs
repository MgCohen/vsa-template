namespace Spike.Core;

public sealed class AppStop {
    readonly object gate = new();
    int? requested;
    Action<int>? listener;

    public void Request(int exitCode = 0) {
        Action<int>? notify;
        lock (gate) {
            if (requested is not null) {
                return;
            }
            requested = exitCode;
            notify = listener;
        }
        notify?.Invoke(exitCode);
    }

    internal int? Listen(Action<int> listener) {
        lock (gate) {
            this.listener = listener;
            return requested;
        }
    }
}

namespace Umpire.Kernels;

sealed class AppSignals : IDisposable {
    static readonly List<AppSignals> listening = [];
    readonly AppStop stop;

    AppSignals(AppStop stop) {
        this.stop = stop;
        Console.CancelKeyPress += OnCancel;
        lock (listening) {
            listening.Add(this);
        }
    }

    public static AppSignals Listen(AppStop stop) => new(stop);

    public void Dispose() {
        lock (listening) {
            listening.Remove(this);
        }
        Console.CancelKeyPress -= OnCancel;
    }

    internal static void InterruptAllListeners() {
        AppSignals[] all;
        lock (listening) {
            all = [.. listening];
        }
        foreach (var signals in all) {
            signals.stop.Request();
        }
    }

    void OnCancel(object? sender, ConsoleCancelEventArgs e) {
        e.Cancel = true;
        stop.Request();
    }
}

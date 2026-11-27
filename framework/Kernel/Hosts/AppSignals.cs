namespace Umpire.Kernels;

sealed class AppSignals : IDisposable {
    static readonly List<AppSignals> listening = [];
    readonly Action signal;

    AppSignals(Action signal) {
        this.signal = signal;
        Console.CancelKeyPress += OnCancel;
        lock (listening) {
            listening.Add(this);
        }
    }

    public static AppSignals Listen(AppStop stop) => new(() => stop.Request());

    public static AppSignals Listen(Action signal) => new(signal);

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
            signals.signal();
        }
    }

    void OnCancel(object? sender, ConsoleCancelEventArgs e) {
        e.Cancel = true;
        signal();
    }
}

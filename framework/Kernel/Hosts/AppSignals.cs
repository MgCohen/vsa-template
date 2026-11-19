namespace Umpire.Kernels;

#if NET
using System.Runtime.InteropServices;
#endif

sealed class AppSignals : IDisposable {
    static readonly List<AppSignals> listening = [];
    readonly AppStop stop;
#if NET
    readonly PosixSignalRegistration terminate;
#endif

    AppSignals(AppStop stop) {
        this.stop = stop;
        Console.CancelKeyPress += OnCancel;
#if NET
        terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnTerminate);
#endif
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
#if NET
        terminate.Dispose();
#endif
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

#if NET
    void OnTerminate(PosixSignalContext context) {
        context.Cancel = true;
        stop.Request();
    }
#endif
}

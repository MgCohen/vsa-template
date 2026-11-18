namespace Umpire.Kernels;

#if NET
using System.Runtime.InteropServices;
#endif

sealed class AppSignals : IDisposable {
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
    }

    public static AppSignals Listen(AppStop stop) => new(stop);

    public void Dispose() {
        Console.CancelKeyPress -= OnCancel;
#if NET
        terminate.Dispose();
#endif
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

namespace Umpire.Cli.Plugins.Commands.Infrastructure;

using System.Diagnostics;

public static class ChildProcess {
    public const int StoppedExitCode = 130;

    public static async Task<int> Stream(
        ProcessStartInfo start, IReadOnlyList<string> args, TimeSpan stopTime, CancellationToken ct) {
        if (ct.IsCancellationRequested) {
            return StoppedExitCode;
        }
        foreach (var arg in args) {
            start.ArgumentList.Add(arg);
        }
        using var process = Process.Start(start)!;
        try {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        } catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            await Stop(process, stopTime).ConfigureAwait(false);
        }
        return process.ExitCode;
    }

    static async Task Stop(Process process, TimeSpan stopTime) {
        ChildSignal.Terminate(process);
        using var wait = new CancellationTokenSource(stopTime);
        try {
            await process.WaitForExitAsync(wait.Token).ConfigureAwait(false);
            return;
        } catch (OperationCanceledException) {
        }
        try {
            process.Kill(entireProcessTree: true);
        } catch (InvalidOperationException) {
        }
        await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
    }
}

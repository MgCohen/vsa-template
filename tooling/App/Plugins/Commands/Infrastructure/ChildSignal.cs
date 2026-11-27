namespace Umpire.Cli.Plugins.Commands.Infrastructure;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

static class ChildSignal {
    public static void Terminate(Process process) {
        if (OperatingSystem.IsWindows()) {
            return;
        }
        try {
            if (process.HasExited) {
                return;
            }
            var start = new ProcessStartInfo("kill") { RedirectStandardError = true };
            start.ArgumentList.Add("-TERM");
            start.ArgumentList.Add(process.Id.ToString(CultureInfo.InvariantCulture));
            using var kill = Process.Start(start);
            kill?.WaitForExit();
        } catch (Exception e) when (e is InvalidOperationException or Win32Exception) {
        }
    }
}

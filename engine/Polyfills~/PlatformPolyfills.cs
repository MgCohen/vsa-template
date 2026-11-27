namespace Umpire.Engine.Polyfills;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

static class PlatformPolyfills {
    public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static string TrimEndingDirectorySeparator(string path) =>
        path.Length > 1 && IsSeparator(path[path.Length - 1]) && !string.Equals(Path.GetPathRoot(path), path, StringComparison.Ordinal)
            ? path.Substring(0, path.Length - 1)
            : path;

    public static string Sha256Hex(byte[] bytes) {
        using var sha = SHA256.Create();
        return Hex(sha.ComputeHash(bytes));
    }

    public static string Sha256Hex(Stream stream) {
        using var sha = SHA256.Create();
        return Hex(sha.ComputeHash(stream));
    }

    public static string Hex(byte[] bytes) {
        var text = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes) {
            text.Append(value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
        }
        return text.ToString();
    }

    public static void MoveOver(string source, string destination) {
        if (!File.Exists(destination)) {
            File.Move(source, destination);
            return;
        }
        try {
            File.Replace(source, destination, null);
        } catch (IOException) when (IsWindows && File.Exists(source)) {
            File.Delete(destination);
            File.Move(source, destination);
        }
    }

    public static bool IsAsciiLetter(char value) => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    public static string CommandLine(IEnumerable<string> arguments) => string.Join(" ", arguments.Select(Quoted));

    public static Task WaitForExitAsync(this Process process, CancellationToken cancellation) {
        var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => exited.TrySetResult(true);
        if (process.HasExited) {
            exited.TrySetResult(true);
        }
        var registration = cancellation.Register(() => exited.TrySetCanceled(cancellation));
        return exited.Task.ContinueWith(
            done => {
                registration.Dispose();
                if (!done.IsCanceled) {
                    process.WaitForExit();
                }
                return done;
            },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    static bool IsSeparator(char value) => value == Path.DirectorySeparatorChar || value == Path.AltDirectorySeparatorChar;

    static string Quoted(string argument) {
        if (argument.Length > 0 && argument.IndexOfAny([' ', '\t', '\n', '\v', '"']) < 0) {
            return argument;
        }
        var text = new StringBuilder("\"");
        for (var at = 0; at < argument.Length; at++) {
            var slashes = 0;
            while (at < argument.Length && argument[at] == '\\') {
                slashes++;
                at++;
            }
            if (at == argument.Length) {
                text.Append('\\', slashes * 2);
            } else if (argument[at] == '"') {
                text.Append('\\', (slashes * 2) + 1).Append('"');
            } else {
                text.Append('\\', slashes).Append(argument[at]);
            }
        }
        return text.Append('"').ToString();
    }
}

using System.Diagnostics;
using Umpire.Cli.Plugins.Commands.Infrastructure;

namespace Cli.Tests;

[Platform("Unix")]
public sealed class ChildProcessTests {
    static readonly TimeSpan LongStop = TimeSpan.FromSeconds(30);
    static readonly TimeSpan ShortStop = TimeSpan.FromMilliseconds(200);

    [Test]
    public async Task WithNoStopSignalTheExitCodeIsTheExitCodeOfTheChild() {
        var exitCode = await ChildProcess.Stream(new ProcessStartInfo("sh"), ["-c", "exit 3"], LongStop, CancellationToken.None);

        Assert.That(exitCode, Is.EqualTo(3));
    }

    [Test]
    public async Task AfterAStopSignalTheChildGetsSigtermAndTheToolGivesItsExitCode() {
        using var signal = new CancellationTokenSource();
        var started = Stopwatch.StartNew();
        signal.CancelAfter(TimeSpan.FromMilliseconds(300));

        var exitCode = await ChildProcess.Stream(
            new ProcessStartInfo("sh"), ["-c", "trap 'exit 7' TERM; sleep 30 & wait"], LongStop, signal.Token);

        Assert.That(exitCode, Is.EqualTo(7));
        Assert.That(started.Elapsed, Is.LessThan(TimeSpan.FromSeconds(20)));
    }

    [Test]
    public async Task AChildThatDoesNotStopInTimeIsKilledAndItsExitCodeIsNotZero() {
        using var signal = new CancellationTokenSource();
        var started = Stopwatch.StartNew();
        signal.CancelAfter(TimeSpan.FromMilliseconds(300));

        var exitCode = await ChildProcess.Stream(
            new ProcessStartInfo("sh"), ["-c", "trap '' TERM; sleep 30"], ShortStop, signal.Token);

        Assert.That(exitCode, Is.Not.Zero);
        Assert.That(started.Elapsed, Is.LessThan(TimeSpan.FromSeconds(20)));
    }

    [Test]
    public async Task AfterAStopSignalNoChildStarts() {
        using var signal = new CancellationTokenSource();
        await signal.CancelAsync();

        var exitCode = await ChildProcess.Stream(new ProcessStartInfo("sh"), ["-c", "exit 0"], LongStop, signal.Token);

        Assert.That(exitCode, Is.EqualTo(ChildProcess.StoppedExitCode));
    }
}

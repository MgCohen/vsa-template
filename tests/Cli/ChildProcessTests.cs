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
    public async Task AfterAStopSignalTheToolWaitsForTheChildAndGivesItsExitCode() {
        using var signal = new CancellationTokenSource();
        await signal.CancelAsync();

        var exitCode = await ChildProcess.Stream(new ProcessStartInfo("sh"), ["-c", "sleep 1; exit 7"], LongStop, signal.Token);

        Assert.That(exitCode, Is.EqualTo(7));
    }

    [Test]
    public async Task AChildThatDoesNotStopInTimeIsKilledAndItsExitCodeIsNotZero() {
        using var signal = new CancellationTokenSource();
        var started = Stopwatch.StartNew();
        signal.CancelAfter(TimeSpan.FromMilliseconds(100));

        var exitCode = await ChildProcess.Stream(new ProcessStartInfo("sleep"), ["30"], ShortStop, signal.Token);

        Assert.That(exitCode, Is.Not.Zero);
        Assert.That(started.Elapsed, Is.LessThan(TimeSpan.FromSeconds(20)));
    }
}

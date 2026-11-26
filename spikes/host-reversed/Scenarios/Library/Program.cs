namespace Greeter.Library;

using Greeter.Features.Greeting;
using Greeter.Features.Greeting.Slices.Greet;
using Greeter.Library.Features.Probes;
using Greeter.Library.Features.Probes.Slices.Fail;
using Greeter.Library.Features.Probes.Slices.Quit;
using Greeter.Library.Features.Probes.Slices.Slow;
using Greeter.Library.Features.Probes.Slices.Unhandled;
using Spike.Hosts.Local;
using Spike.Kernels;

static class Program {
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    static async Task<int> Main() {
        Probe.Section("1. Default module scan");
        var scanned = new GreeterApp();
        var scannedHost = new LocalHost();
        var scannedRun = scanned.Run(scannedHost);
        Probe.Show("Send", () => scannedHost.Send(new GreetRequest("Ada")).Text);
        Probe.Show("Send of a probe request (not scanned)", () => scannedHost.Send(new SlowRequest(1)).Text);
        scanned.Stop();
        Console.WriteLine($"Exit code: {await scannedRun}");

        Probe.Section("2. Before Run");
        var app = new GreeterApp { Modules = new FixedModules(new GreetingModule(), new ProbesModule()) };
        var local = new LocalHost();
        Console.WriteLine($"State: {app.State}");
        Probe.Show("Send before Run", () => local.Send(new GreetRequest("Ada")).Text);
        Probe.Show("Stop before Run", () => {
            app.Stop(5);
            return $"no effect, state {app.State}";
        });

        Probe.Section("3. First run");
        var run = app.Run(local);
        Console.WriteLine($"State: {app.State}, task completed: {run.IsCompleted}");
        Probe.Show("Sync Send", () => local.Send(new GreetRequest("Ada")).Text);
        await Probe.ShowAsync("SendAsync", async () => (await local.SendAsync(new GreetRequest("Grace"))).Text);
        await Probe.ShowAsync("Several sends", async () => {
            var names = new[] { "Alan", "Barbara", "Edsger" };
            var responses = await Task.WhenAll(names.Select(name => local.SendAsync(new GreetRequest(name))));
            return string.Join(" | ", responses.Select(response => response.Text));
        });
        Probe.Show("Default request value", () => local.Send(new GreetRequest()).Text);
        Probe.Show("Run while running", () => app.Run(local).Status.ToString());
        Probe.Show("Unknown request", () => local.Send(new UnhandledRequest()));
        Probe.Show("Handler throws", () => local.Send(new FailRequest("The name is empty.")).Text);
        Probe.Show("Cancelled send", () => local.Send(new SlowRequest(1000), new CancellationToken(true)).Text);
        app.Stop();
        Console.WriteLine($"After Stop: state {app.State}, task completed: {run.IsCompleted}, exit code {await run}");
        Probe.Show("Send after Stop", () => local.Send(new GreetRequest("Ada")).Text);

        Probe.Section("4. Second run of the same app instance");
        var second = new LocalHost();
        var secondRun = app.Run(second);
        Probe.Show("Send on the new host", () => second.Send(new GreetRequest("Ada")).Text);
        Probe.Show("Send on the host of run 1", () => local.Send(new GreetRequest("Ada")).Text);
        Probe.Show("Stop from inside the app", () => second.Send(new QuitRequest(7)).Text);
        Console.WriteLine($"Exit code: {await secondRun}, state {app.State}");

        Probe.Section("5. Concurrent sends");
        var thirdRun = app.Run(local);
        var syncOk = 0;
        Parallel.For(0, 500, index => {
            if (local.Send(new GreetRequest($"n{index}")).Text == $"Hello, n{index}.") {
                Interlocked.Increment(ref syncOk);
            }
        });
        Console.WriteLine($"Parallel sync sends: {syncOk} of 500 correct");
        var asyncResponses = await Task.WhenAll(Enumerable.Range(0, 500)
            .Select(index => Task.Run(() => local.SendAsync(new GreetRequest($"n{index}")))));
        Console.WriteLine($"Concurrent SendAsync: {asyncResponses.Count(response => response.Text.StartsWith("Hello, n", StringComparison.Ordinal))} of 500 correct");
        var inFlight = Enumerable.Range(0, 20).Select(_ => Outcome(local.SendAsync(new SlowRequest(100)))).ToList();
        app.Stop();
        Console.WriteLine($"Run task completed: {thirdRun.IsCompleted}, sends done at that time: {inFlight.Count(send => send.IsCompleted)} of 20");
        Console.WriteLine($"Stop with 20 slow sends in flight, exit code {await thirdRun}:");
        foreach (var group in (await Task.WhenAll(inFlight)).GroupBy(text => text)) {
            Console.WriteLine($"  {group.Count()} x {group.Key}");
        }

        Probe.Section("6. Single-threaded SynchronizationContext");
        var fourthRun = app.Run(local);
        Console.WriteLine("A sync Send, handler completes at once: " + SingleThreadContext.Run(
            () => Task.FromResult(local.Send(new GreetRequest("Ada")).Text), Timeout));
        Console.WriteLine("B await SendAsync, handler awaits: " + SingleThreadContext.Run(
            async () => (await local.SendAsync(new SlowRequest(50))).Text, Timeout));
        Console.WriteLine("C sync Send through Task.Run, handler awaits: " + SingleThreadContext.Run(
            () => Task.FromResult(Task.Run(() => local.Send(new SlowRequest(50))).GetAwaiter().GetResult().Text), Timeout));
        Console.WriteLine("D sync Send, handler awaits: " + SingleThreadContext.Run(
            () => Task.FromResult(local.Send(new SlowRequest(50)).Text), Timeout));
        app.Stop();
        Console.WriteLine($"Exit code: {await fourthRun}");
        return 0;
    }

    static async Task<string> Outcome(Task<SlowResponse> send) {
        try {
            return (await send).Text;
        } catch (Exception e) {
            return $"{e.GetType().Name}: {e.Message}";
        }
    }
}

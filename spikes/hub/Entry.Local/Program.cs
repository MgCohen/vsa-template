using Ping;
using Ping.Features.Pong.Slices.Ping;
using Ping.Features.Pong.Slices.Wait;
using Spike.Hosts.Local;

Console.WriteLine("== 1. await using: one sync and one async send");
await using (var instance = new PingApp().Run(new LocalHost())) {
}

var local = new LocalHost();
await using (var instance = new PingApp().Run(local)) {
    Console.WriteLine(local.Send(new PingRequest(5)));
    Console.WriteLine(await local.SendAsync(new PingRequest(6)));
}

Console.WriteLine("== 2. Stop drains the running sends");
var drained = new LocalHost();
var second = new PingApp().Run(drained);
var running = Enumerable.Range(1, 5).Select(i => drained.SendAsync(new WaitRequest(300))).ToArray();
await Task.Delay(50);
second.Stop(3);
try {
    await drained.SendAsync(new PingRequest(1));
} catch (InvalidOperationException e) {
    Console.WriteLine($"send after Stop: {e.Message}");
}
Console.WriteLine($"exit code {await second}, done {running.Count(t => t.IsCompletedSuccessfully)} of {running.Length}");

Console.WriteLine("== 3. A stop request during the start is not lost");
Console.WriteLine($"exit code {await new PingApp().Run(new EarlyStopHost(), new LocalHost())}");

Console.WriteLine("== 4. A failed host work stops the app");
Console.WriteLine($"exit code {await new PingApp().Run(new FailingHost())}");

Console.WriteLine("== 5. A send that runs longer than the grace time is cancelled");
var slow = new LocalHost();
var third = new PingApp().Run(slow);
var longSend = slow.SendAsync(new WaitRequest(10000));
await Task.Delay(50);
third.Stop();
Console.WriteLine($"exit code {await third}");
try {
    await longSend;
} catch (InvalidOperationException e) {
    Console.WriteLine($"the long send: {e.Message}");
}

Console.WriteLine("== 6. A host that fails at once is still closed");
Console.WriteLine($"exit code {await new PingApp().Run(new LocalHost(), new FailsAtOnceHost())}");

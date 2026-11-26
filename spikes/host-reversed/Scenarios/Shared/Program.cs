using Greeter;
using Greeter.Features.Greeting;
using Greeter.Features.Greeting.Slices.Greet;
using Greeter.Shared;
using Greeter.Shared.Features.Counting;
using Greeter.Shared.Features.Counting.Slices.Count;
using Spike.Hosts.Console;
using Spike.Hosts.Local;
using Spike.Kernels;

var local = new LocalHost();
var app = new GreeterApp { Modules = new FixedModules(new GreetingModule(), new CountingModule()) };

var openDelayMs = int.Parse(new CommandLine(args).Option("opendelay") ?? "0", System.Globalization.CultureInfo.InvariantCulture);
var run = app.Run(new TraceHost("first"), new ConsoleHost(args), local, new TraceHost("last", openDelayMs));
Console.WriteLine($"[main] Run returned, state {app.State}");

var inflight = Task.FromResult(new CountResponse(0, "no send"));
try {
    Console.WriteLine($"[local] {await local.SendAsync(new GreetRequest("local"))}");
    var parallel = await Task.WhenAll(Enumerable.Range(1, 3).Select(i => local.SendAsync(new CountRequest($"local {i}"))));
    foreach (var response in parallel) {
        Console.WriteLine($"[local] {response}");
    }
    inflight = local.SendAsync(new CountRequest("local in-flight", 1000));
} catch (InvalidOperationException e) {
    Console.WriteLine($"[local] send failed while the app stops: {e.Message}");
}

var exitCode = await run;
Console.WriteLine($"[main] run ended, exit code {exitCode}, state {app.State}");

try {
    Console.WriteLine($"[local] {await inflight}");
} catch (InvalidOperationException e) {
    Console.WriteLine($"[local] in-flight send failed: {e.Message}");
}

try {
    local.Send(new CountRequest("local late"));
} catch (InvalidOperationException e) {
    Console.WriteLine($"[local] late send failed: {e.Message}");
}

return exitCode;

using System.Diagnostics;
using SpikeApp;
using SpikeApp.Features.Describe.Slices.DescribeType;
using Umpire.Kernels;

var watch = Stopwatch.StartNew();
var app = new SpikeApp<LocalHost>();
app.Start([]);
Console.WriteLine($"compose: {watch.Elapsed.TotalMilliseconds:F1} ms");
for (var i = 0; i < 3; i++) {
    watch.Restart();
    var result = app.Send(new DescribeTypeRequest("Foo", i));
    Console.WriteLine($"send {i}: {watch.Elapsed.TotalMilliseconds:F3} ms -> {((DescribeTypeResponse)result.Value!).Message} error={result.Error}");
}
watch.Restart();
for (var i = 0; i < 1000; i++) {
    app.Send(new DescribeTypeRequest("Foo", i));
}
Console.WriteLine($"send x1000: {watch.Elapsed.TotalMilliseconds / 1000:F4} ms each");
app.Stop();

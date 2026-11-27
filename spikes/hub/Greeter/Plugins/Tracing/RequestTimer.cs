namespace Greeter.Plugins.Tracing;

using System.Diagnostics;
using Spike.Core;
using Spike.Pack;

public sealed class RequestTimer : PipelineStep<IRequest> {
    protected override async Task<MessageResult> Run(IRequest message, MessageNext next) {
        var clock = Stopwatch.StartNew();
        var result = await next().ConfigureAwait(false);
        await System.Console.Error.WriteLineAsync($"[request] {message.GetType().Name} took {clock.Elapsed.TotalMilliseconds:0.00} ms").ConfigureAwait(false);
        return result;
    }
}

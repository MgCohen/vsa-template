namespace Ping.Plugins.Tracing;

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Spike.Core;
using Spike.Pack;

public sealed class TraceStep : IHubStep {
    public async Task<MessageResult> Run(object message, MessageNext next) {
        Console.Error.WriteLine($"[hub] {message}");
        return await next().ConfigureAwait(false);
    }
}

public sealed class RequestTimer : PipelineStep<IRequest> {
    public override async Task<MessageResult> Run(IRequest message, MessageNext next) {
        var clock = Stopwatch.StartNew();
        var result = await next().ConfigureAwait(false);
        Console.Error.WriteLine($"[request] {message.GetType().Name} took {clock.Elapsed.TotalMilliseconds:0.00} ms");
        return result;
    }
}

public sealed class TracingPlugin : Plugin {
    public override void Configure(IServiceCollection services) {
        services.AddHubStep<TraceStep>();
        services.AddStep<RequestTimer, IRequest>();
    }
}

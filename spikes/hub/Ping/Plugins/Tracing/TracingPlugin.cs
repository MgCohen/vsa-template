namespace Ping.Plugins.Tracing;

using Microsoft.Extensions.DependencyInjection;
using Spike.Core;

public sealed class TracingPlugin : Module {
    protected override void Configure(IServiceCollection services) {
        services.AddHubStep<TraceStep>();
        services.AddPipelineStep<RequestTimer>();
    }
}

namespace Echo;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

// A wrong module: two slices handle the same command, so the host does not start.
public sealed class EchoModule : Module {
    public override void Configure(IServiceCollection services) {
    }
}

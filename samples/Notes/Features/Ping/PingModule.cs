namespace Notes.Features.Ping;

using Microsoft.Extensions.DependencyInjection;
using VSA.Kernel;

// A second sample module with no settings and no database. The host finds it with no change to Program.cs.
public sealed class PingModule : Module {
    public override void Configure(IServiceCollection services) { }
}

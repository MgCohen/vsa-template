using VSA.Module;

namespace Ping;

// A second sample module with no settings and no database. The host finds it with no change to Program.cs.
public sealed class PingModule : Module {
    public override void Configure(ModuleBuilder module) { }
}

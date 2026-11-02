using VSA.Module;

namespace Twin;

// A module root with two Module classes. ModuleDiscovery.CreateModules must give an error for it.
public sealed class TwinModule : Module {
    public override void Configure(ModuleBuilder module) {
    }
}

public sealed class OtherTwinModule : Module {
    public override void Configure(ModuleBuilder module) {
    }
}

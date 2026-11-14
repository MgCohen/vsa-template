using VSA.Module;

namespace Twin;

// Two Module classes in one module namespace. ModuleRoot.CreateModule must give an error for it.
public sealed class TwinModule : Module {
    public override void Configure(ModuleBuilder module) {
    }
}

public sealed class OtherTwinModule : Module {
    public override void Configure(ModuleBuilder module) {
    }
}

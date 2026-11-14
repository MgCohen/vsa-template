using VSA.Module;
using VSA.Kernel;

namespace Twin;

// Two Module classes in one module namespace. AnchorRoot.CreateModule must give an error for it.
public sealed class TwinModule : Module {
    public override void Configure(AnchorBuilder module) {
    }
}

public sealed class OtherTwinModule : Module {
    public override void Configure(AnchorBuilder module) {
    }
}

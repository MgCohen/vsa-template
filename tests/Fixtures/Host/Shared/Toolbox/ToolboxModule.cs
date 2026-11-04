using VSA.Module;

namespace Toolbox;

// A Shared module root with no layers. Each module root of the app gets it from VSA.Sdk.
public sealed class ToolboxModule : Module {
    public override void Configure(ModuleBuilder module) { }
}

using VSA.Module;
using VSA.Kernel;

namespace Toolbox;

// A plugin module root with no layers. Each module root of the app gets it from VSA.Sdk.
public sealed class ToolboxPlugin : Plugin {
    public override void Configure(AnchorBuilder module) { }
}

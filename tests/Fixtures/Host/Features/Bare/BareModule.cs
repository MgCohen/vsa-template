using VSA.Module;
using VSA.Kernel;

namespace Bare;

// A module root with no layer projects. Its root records no layers.
public sealed class BareModule : Module {
    public override void Configure(AnchorBuilder module) {
    }
}

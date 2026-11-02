using VSA.Module;

namespace Shelf;

// A module with request and presenter layers, but no Handlers/Events layer.
public sealed class ShelfModule : Module {
    public override void Configure(ModuleBuilder module) {
    }
}

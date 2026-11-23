namespace Umpire.Kernels;

using Umpire.Architecture;
using Root = Umpire.Architecture.Blocks.Component;

sealed class ComponentArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Part root) {
        root.Add<Root>().InParentFolder().OfType<object>();
    }
}

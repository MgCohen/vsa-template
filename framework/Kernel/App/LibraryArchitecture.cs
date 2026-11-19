namespace Umpire.Kernels;

using Umpire.Architecture;
using Root = Umpire.Architecture.Blocks.Library;

sealed class LibraryArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Block block) {
        block.Add<Root>().InParentFolder().OfType<object>();
    }
}

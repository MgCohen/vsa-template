namespace Umpire.Kernels;

using Umpire.Architecture;
using Root = Umpire.Architecture.Blocks.Library;

public sealed class ApiArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Block block) {
        block.Add<Root>().InParentFolder().OfType<object>();
        block.Add<ApiParts.Concepts>().And<ApiParts.Markers>().And<ApiParts.Options>().And<ApiParts.Parts>()
            .And<ApiParts.Queries>().And<ApiParts.Rules>().And<ApiParts.Scan>().And<ApiParts.Testing>();
    }
}

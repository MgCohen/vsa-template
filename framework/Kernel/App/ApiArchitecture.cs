namespace Umpire.Kernels;

using Umpire.Architecture;
using Root = Umpire.Architecture.Blocks.Component;

sealed class ApiArchitecture : Architecture {
    protected override void Configure(PartOptions defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Part root) {
        root.Add<Root>().InParentFolder().OfType<object>();
        root.Add<ApiParts.Concepts>().And<ApiParts.Markers>().And<ApiParts.Options>().And<ApiParts.Parts>()
            .And<ApiParts.Queries>().And<ApiParts.Rules>().And<ApiParts.Scan>().And<ApiParts.Testing>();
    }
}

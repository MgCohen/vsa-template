namespace Umpire.Kernels;

using Umpire.Architecture;
using Root = Umpire.Architecture.Blocks.Component;

sealed class ApiArchitecture : Architecture {
    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Part root, Part kernel) {
        root.Add<Root>().InParentFolder().OfType<object>();
        root.Add<ApiParts.Concepts>().And<ApiParts.Markers>().And<ApiParts.Options>().And<ApiParts.Parts>()
            .And<ApiParts.Rules>();
    }
}

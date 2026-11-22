namespace Umpire.Kernels;

using Umpire.Architecture;

sealed class KernelArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Part root) {
        root.Add<KernelParts.App>().And<KernelParts.Kernels>().And<KernelParts.Hosts>().And<KernelParts.Operations>()
            .And<KernelParts.Modules>().And<KernelParts.Scan>().And<KernelParts.Factories>().And<KernelParts.Services>()
            .And<KernelParts.Analyzers>();
    }
}

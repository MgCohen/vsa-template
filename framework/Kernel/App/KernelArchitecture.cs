namespace Umpire.Kernels;

using Umpire.Architecture;

sealed class KernelArchitecture : Architecture {
    protected override void Configure(Options defaults) {
        defaults.DefaultPermission = Permission.Allow;
    }

    protected override void Define(Part root) {
        root.Add<FrameworkKernel>().InParentFolder();
    }
}

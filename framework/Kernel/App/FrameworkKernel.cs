namespace Umpire.Kernels;

using Umpire.Architecture;
using KernelBlock = Umpire.Architecture.Blocks.Kernel;

public sealed record FrameworkKernel : KernelBlock {
    public override string Folder => "Kernel";

    protected override void Define(Part part) {
        part.Add<KernelParts.App>().And<KernelParts.Kernels>().And<Hosts>().And<KernelParts.Operations>()
            .And<KernelParts.Modules>().And<KernelParts.Scan>().And<KernelParts.Factories>().And<KernelParts.Services>()
            .And<KernelParts.Analyzers>();
    }
}

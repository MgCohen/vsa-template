namespace Umpire.Kernels;

using Umpire.Architecture;

public abstract class Architecture<TKernel> : Architecture where TKernel : Kernel {
    protected override void Configure(ArchitectureConfig config) => config.Roots.Add<Blocks.Core>();
}

namespace Umpire.Kernels;

using Umpire.Architecture;

public abstract class CoreArchitecture : Architecture {
    protected override void Configure(ArchitectureConfig config) => config.Roots.Add<Core>();
}

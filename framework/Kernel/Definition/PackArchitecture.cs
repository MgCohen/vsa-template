namespace Umpire.Kernels;

using Umpire.Architecture;
using DefinitionRoot = Umpire.Architecture.Blocks.Definition;
using KernelBlock = Umpire.Architecture.Blocks.Kernel;

public sealed class PackArchitecture : Architecture {
    readonly Architecture? packed;

    public PackArchitecture() { }

    public PackArchitecture(Architecture packed) => this.packed = packed;

    protected override void Configure(ArchitectureConfig config) {
        config.Defaults.UseSameConcept = true;
    }

    protected override void Define(Part root, Part kernel) {
        packed?.DefineKernel(kernel);
        foreach (var part in kernel.Walk()) {
            part.ClearTypes();
        }
        if (kernel.Children.Count == 0) {
            root.Add<PackParts.Kernel>();
        }
        root.Add<PackParts.Tests>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<DefinitionRoot>().HasTypes<Architecture>().AtLeast(1);
        rules.For<DefinitionRoot>().Uses<KernelBlock>();
        rules.For<PackParts.Tests>().Uses<DefinitionRoot>().And<KernelBlock>();
        if (packed is not null) {
            rules.For<KernelBlock>().Uses<KernelBlock>();
            rules.For<KernelBlock>(Depth.Self).HasTypes<Kernel>().AtMost(1);
        }
    }
}

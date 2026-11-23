namespace Umpire.Kernels;

using Umpire.Architecture;
using KernelBlock = Umpire.Architecture.Blocks.Kernel;

public sealed class PackArchitecture : Architecture {
    readonly Architecture? packed;

    public PackArchitecture() { }

    public PackArchitecture(Architecture packed) => this.packed = packed;

    protected override void Configure(Options defaults) {
        defaults.UseSameConcept = true;
    }

    protected override void Define(Part root) {
        root.Add<RootClass>().OfType<Pack>(required: true);
        root.Add<PackParts.Architecture>();
        var kernel = packed?.KernelOfPack is { } record ? root.AddOf(record, "Kernel") : root.Add<PackParts.Kernel>();
        foreach (var part in kernel.Walk()) {
            part.ClearTypes();
        }
        root.Add<PackParts.Tests>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<RootClass>().Uses<PackParts.Architecture>().And<KernelBlock>();
        rules.For<PackParts.Architecture>().Uses<KernelBlock>();
        rules.For<PackParts.Tests>().Uses<PackParts.Architecture>().And<KernelBlock>();
    }
}

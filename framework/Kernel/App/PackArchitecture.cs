namespace Umpire.Kernels;

using Umpire.Architecture;
using PackClass = Umpire.Architecture.Blocks.Module;

public sealed class PackArchitecture : Architecture {
    readonly Architecture? packed;

    public PackArchitecture() { }

    public PackArchitecture(Architecture packed) => this.packed = packed;

    protected override void Configure(Options defaults) {
        defaults.UseSameConcept = true;
    }

    protected override void Define(Part root) {
        root.Add<PackClass>().OfType<Pack>(required: true);
        root.Add<PackParts.Architecture>();
        packed?.DefineKernelPart(root.Add<PackParts.Kernel>());
        if (packed is null) {
            root.Add<PackParts.Kernel>();
        }
        root.Add<PackParts.Tests>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<PackClass>().Uses<PackParts.Architecture>().And<PackParts.Kernel>();
        rules.For<PackParts.Architecture>().Uses<PackParts.Kernel>();
        rules.For<PackParts.Tests>().Uses<PackParts.Architecture>().And<PackParts.Kernel>();
        packed?.AddKernelRules(rules);
    }
}

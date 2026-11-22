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

    protected override void Define(Block block) {
        block.Add<PackClass>().OfType<Pack>(required: true);
        block.Add<PackParts.Architecture>();
        packed?.DefineKernelPart(block.Add<PackParts.Kernel>());
        if (packed is null) {
            block.Add<PackParts.Kernel>();
        }
        block.Add<PackParts.Tests>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<PackClass>().Uses<PackParts.Architecture>().And<PackParts.Kernel>();
        rules.For<PackParts.Architecture>().Uses<PackParts.Kernel>();
        rules.For<PackParts.Tests>().Uses<PackParts.Architecture>().And<PackParts.Kernel>();
        packed?.AddKernelRules(rules);
    }
}

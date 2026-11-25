namespace PackVariant.Packs.Ecs.Definition;

using PackVariant.Packs.Ecs.Kernel;
using PackVariant.Packs.Ecs.Kernel.Domain;
using Umpire.Architecture;
using Umpire.Kernels.Blocks;
using Umpire.Vsa.Definition;

public sealed class EcsArchitecture : VsaArchitecture<EcsKernel> {
    protected override void Define(Part root, Part kernel) {
        var ecs = root.AddGroup<EcsFeature>();
        ecs.Add<Components>();
        ecs.Add<Systems>().OfType<EcsSystem>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Systems>().Uses<Components>().And<Pack>();
        rules.For<EcsFeature>(Depth.Self).Uses<EcsFeature>(Access.Internal);
        rules.For<EcsFeature>().Has<Systems>().AtLeast(1);
    }
}

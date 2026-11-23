namespace PackVariant.Packs.Ecs.Architecture;

using PackVariant.Packs.Ecs.Kernel.Domain;
using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Vsa.Definition;

public sealed class EcsArchitecture : VsaArchitecture {
    protected override void Define(Part root, Part kernel) {
        var ecs = root.AddGroup<EcsFeature>();
        ecs.Add<RootClass>().OfType<Umpire.Kernels.Module>();
        ecs.Add<Components>();
        ecs.Add<Systems>().OfType<EcsSystem>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Systems>().Uses<Components>().And<Pack>();
        rules.For<RootClass>().Under<EcsFeature>().Uses<EcsFeature>(Access.Internal);
        rules.For<EcsFeature>().Has<RootClass>().Exactly(1);
        rules.For<EcsFeature>().Has<Systems>().AtLeast(1);
    }
}

namespace PackVariant.Packs.Ecs.Architecture;

using PackVariant.Packs.Ecs.Kernel;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class EcsArchitecture : VsaArchitecture {
    protected override void Define(Block block) {
        base.Define(block);
        var ecs = block.AddGroup<EcsFeature>();
        ecs.Add<Module>().OfType<Umpire.Kernel.Module>();
        ecs.Add<Components>();
        ecs.Add<Systems>().OfType<EcsSystem>();
    }

    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<Systems>().Uses<Components>().And<Pack>();
        rules.For<Module>().Under<EcsFeature>().Uses<EcsFeature>(Access.Internal);
        rules.For<EcsFeature>().Has<Module>().Exactly(1);
        rules.For<EcsFeature>().Has<Systems>().AtLeast(1);
    }
}

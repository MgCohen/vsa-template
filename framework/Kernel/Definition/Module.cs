namespace Umpire.Kernels.Blocks;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record Module : Component {
    protected override void Rules(RuleBook rules) => rules.For<Module>(Depth.Self).HasTypes<Kernels.Module>().Exactly(1);
}

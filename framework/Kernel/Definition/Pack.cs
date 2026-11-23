namespace Umpire.Kernels.Blocks;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record Pack : Block {
    protected override void Rules(RuleBook rules) => rules.For<Pack>(Depth.Self).HasTypes<Kernels.Pack>().AtLeast(1);
}

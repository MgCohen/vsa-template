namespace Umpire.Kernels.Blocks;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record Pack : Block {
    protected override void Rules(RuleBook rules) => rules.For<Pack>().HasTypes<Architecture>().AtLeast(1);
}

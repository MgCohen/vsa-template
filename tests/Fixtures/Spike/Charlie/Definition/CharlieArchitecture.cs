namespace Spike.Charlie.Definition;

using Spike.Alpha.Definition;
using Umpire.Architecture;
using static Spike.Charlie.Definition.CharlieParts;

public sealed class CharlieArchitecture : AlphaArchitecture {
    protected override void Define(Block block) {
        base.Define(block);
        block.Add<C1>();
    }

    protected override void Rules(RuleBook rules) {
        base.Rules(rules);
        rules.For<C1>().Uses<AlphaParts.A2>();
    }
}

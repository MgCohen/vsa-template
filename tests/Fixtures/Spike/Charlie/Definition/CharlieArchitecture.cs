namespace Spike.Charlie.Definition;

using Spike.Alpha.Definition;
using Spike.Bravo.Definition;
using Umpire.Architecture;
using static Spike.Charlie.Definition.CharlieParts;

public sealed class CharlieArchitecture : AlphaArchitecture {
    protected override void Define(Part root) {
        root.Add<C1>();
        root.For<Zulu>().Add<Z3>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<C1>().Uses<AlphaParts.A2>();
        rules.For<Z3>().Uses<Zulu.Z1>();
    }
}

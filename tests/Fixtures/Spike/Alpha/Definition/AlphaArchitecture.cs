namespace Spike.Alpha.Definition;

using Spike.Bravo.Definition;
using Umpire.Architecture;
using static Spike.Alpha.Definition.AlphaParts;

public class AlphaArchitecture : Architecture {
    protected override void Configure(Options defaults) => defaults.DefaultPermission = Permission.Deny;

    protected override void Define(Part root) {
        root.Add<AlphaParts.Definition>().And<A1>().And<Bravo>();
        root.Add<A2>().Add<Store>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<A2>().Uses<A1>().And<Store>();
        rules.For<A1>().Uses<Bravo.B1>();
    }
}

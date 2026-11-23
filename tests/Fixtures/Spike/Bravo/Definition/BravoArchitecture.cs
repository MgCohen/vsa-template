namespace Spike.Bravo.Definition;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public sealed class BravoArchitecture : Architecture {
    protected override void Configure(PartOptions defaults) => defaults.DefaultPermission = Permission.Deny;

    protected override void Define(Part root) {
        root.Add<BravoParts.Definition>().And<B1>().And<Zulu>();
        root.AddGroup<Plugin>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Plugin>().Uses<Zulu>();
    }

    public sealed record B1 : Concept;

    public sealed record Plugin : Block {
        public override string GroupFolder => "Plugins";

        public override IReadOnlyList<Type>? OwnedBy => [];

        protected override void Define(Part part) => part.Add<B1>();
    }
}

namespace Spike.Bravo.Definition;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public sealed record Zulu : Block {
    protected override void Define(Part part) => part.Add<Z1>().And<Z2>();

    protected override void Rules(RuleBook rules) => rules.For<Z2>().Uses<Z1>();

    public sealed record Z1 : Concept;

    public sealed record Z2 : Concept;
}

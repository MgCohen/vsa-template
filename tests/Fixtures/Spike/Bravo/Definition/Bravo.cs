namespace Spike.Bravo.Definition;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public sealed record Bravo : Block {
    protected override void Define(Part part) => part.Add<B1>().And<B2>();

    protected override void Rules(RuleBook rules) => rules.For<B2>().Uses<B1>();

    public sealed record B1 : Concept;

    public sealed record B2 : Concept;
}

namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

sealed class BaseLevelCallRule : KernelRule {
    static readonly string[] Levels = ["Configure", "Define", "Rules"];

    static readonly string[] DefinitionBases = ["Umpire.Architecture.Architecture", "Umpire.Architecture.Blocks.Block", "Umpire.Architecture.Concept"];

    public override int Number => 7;

    public override string Title => "A level of a definition does not call its base level";

    public override string Message =>
        "{0}.{1} calls base.{1}. The engine calls {1} of each class in the inheritance chain, from the base down, so the base level runs two times. Remove the call to base.{1}.";

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) => file.BaseCalls
        .Where(call => Levels.Contains(call.Called) && call.IsOverride && call.Method == call.Called
            && call.Bases.Any(DefinitionBases.Contains))
        .Select(call => Found(call.Place, call.Type, call.Method));
}

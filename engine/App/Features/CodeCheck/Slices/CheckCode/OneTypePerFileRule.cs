namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

sealed class OneTypePerFileRule : KernelRule {
    public const string AllowSameNameKey = "allow_same_name";

    public override int Number => 4;

    public override IReadOnlyList<RuleOption> Options { get; } = [RuleOption.Bool(AllowSameNameKey, true)];

    public override string Title => "A file has one top-level type";

    public override string Message =>
        "{0} is a second top-level type in the file of {1}. Move {0} to its own file, {0}.cs.";

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) {
        var types = file.TopLevelTypes;
        if (types.Count < 2 || options.Bool(AllowSameNameKey) is not { } allowSameName) {
            return [];
        }
        var first = types[0].Name;
        return types.Skip(1).Where(type => !allowSameName || type.Name != first).Select(type => Found(type.Place, type.Name, first));
    }
}

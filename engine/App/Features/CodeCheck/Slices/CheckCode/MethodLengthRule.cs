namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

sealed class MethodLengthRule : LimitRule {
    public override int Number => 2;

    public override string Title => "A method has at most the maximum number of lines";

    public override string Message =>
        "{0} has {1} lines, and the limit is {2}. Move a part of it to a new method.";

    protected override int DefaultMax => 40;

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) =>
        Max(options) is { } max
            ? file.Methods.Where(method => method.Lines > max).Select(method => Found(method.Name.Place, method.Name.Name, method.Lines, max))
            : [];
}

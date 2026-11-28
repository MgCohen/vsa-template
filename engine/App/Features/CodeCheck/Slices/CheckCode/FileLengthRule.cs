namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

sealed class FileLengthRule : LimitRule {
    public override int Number => 3;

    public override string Title => "A file has at most the maximum number of lines";

    public override string Message =>
        "The file has {0} lines, and the limit is {1}. Move a type or a part of a type to a new file.";

    protected override int DefaultMax => 300;

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) {
        if (Max(options) is not { } max) {
            return [];
        }
        var lines = file.Lines;
        var count = lines.Count > 0 && lines[lines.Count - 1] == 0 ? lines.Count - 1 : lines.Count;
        return count <= max ? [] : [Found(new CodePlace(file.Path, max + 1, 1, lines[max]), count, max)];
    }
}

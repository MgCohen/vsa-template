namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

sealed class LineLengthRule : LimitRule {
    public override int Number => 1;

    public override string Title => "A line has at most the maximum number of characters";

    public override string Message =>
        "The line has {0} characters, and the limit is {1}. Break the line.";

    protected override int DefaultMax => 120;

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) {
        if (Max(options) is not { } max) {
            yield break;
        }
        for (var line = 0; line < file.Lines.Count; line++) {
            var length = file.Lines[line];
            if (length > max) {
                yield return Found(new CodePlace(file.Path, line + 1, max + 1, length - max), length, max);
            }
        }
    }
}

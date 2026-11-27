namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

public sealed record CodeFinding(CodePlace Place, string Text);

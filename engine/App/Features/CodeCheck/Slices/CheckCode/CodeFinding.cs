namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

sealed record CodeFinding(CodePlace Place, string Text);

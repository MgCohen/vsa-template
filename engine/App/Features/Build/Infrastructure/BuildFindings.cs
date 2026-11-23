namespace Build.Infrastructure;

using System.Globalization;
using System.Text.RegularExpressions;
using Steps.Domain.Shared;

public static partial class BuildFindings {
    const string Kind = @"(?<severity>error|warning) (?<code>[A-Za-z]+\d+): (?<text>.*?)(?: \[[^\]]*\])?$";
    const string LocatedPattern = @"^\s*(?<file>[^:\s][^()]*?)\((?<line>\d+),(?<column>\d+)(?:,\d+,\d+)?\): " + Kind;
    const string UnlocatedPattern = @"^\s*(?<origin>[^()\[\]]+?) : " + Kind;

    public static IReadOnlyList<Finding> Of(IEnumerable<string> lines) =>
        [.. lines.Select(Parse).OfType<Finding>().Distinct()];

    static Finding? Parse(string line) {
        if (Located().Match(line) is { Success: true } located) {
            return new Finding(Code(located), Severity(located), Text(located), located.Groups["file"].Value.Trim(),
                Number(located, "line"), Number(located, "column"));
        }
        return Unlocated().Match(line) is { Success: true } unlocated
            ? new Finding(Code(unlocated), Severity(unlocated), Text(unlocated))
            : null;
    }

    static string Code(Match match) => match.Groups["code"].Value;

    static string Text(Match match) => match.Groups["text"].Value.Trim();

    static FindingSeverity Severity(Match match) =>
        match.Groups["severity"].Value == "error" ? FindingSeverity.Error : FindingSeverity.Warning;

    static int Number(Match match, string group) => int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);

    [GeneratedRegex(LocatedPattern)]
    private static partial Regex Located();

    [GeneratedRegex(UnlocatedPattern)]
    private static partial Regex Unlocated();
}

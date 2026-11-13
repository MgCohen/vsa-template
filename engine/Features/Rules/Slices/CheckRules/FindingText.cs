using System.Text.RegularExpressions;
using Report.Domain.Shared;

namespace Rules.Slices.CheckRules;

static partial class FindingText {
    public static Finding Parse(string text, string appRoot) {
        var match = Located().Match(text);
        if (!match.Success) {
            var bare = Bare().Match(text);
            return bare.Success
                ? new Finding(bare.Groups["code"].Value, Severity(bare.Groups["severity"].Value), $"{bare.Groups["where"].Value}: {bare.Groups["text"].Value}")
                : new Finding("VSA2000", FindingSeverity.Error, text);
        }
        var file = match.Groups["file"].Value;
        var full = Path.IsPathRooted(file) ? file : Path.GetFullPath(Path.Combine(appRoot, file));
        return new Finding(
            match.Groups["code"].Value,
            Severity(match.Groups["severity"].Value),
            match.Groups["text"].Value,
            full,
            int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    static FindingSeverity Severity(string text) => text == "warning" ? FindingSeverity.Warning : FindingSeverity.Error;

    [GeneratedRegex(@"^(?<file>.+?)\((?<line>\d+)\): (?<severity>error|warning) (?<code>[A-Z]+\d+): (?<text>.*)$")]
    private static partial Regex Located();

    [GeneratedRegex(@"^(?<where>[^:]+): (?<severity>error|warning) (?<code>[A-Z]+\d+): (?<text>.*)$")]
    private static partial Regex Bare();
}

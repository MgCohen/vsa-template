using System.Text;
using System.Text.RegularExpressions;

namespace Umpire.Cli.Scaffolding;

static partial class Names {
    static readonly string[] NamesThatBreakTheBuild =
        ["App", "Views", "Pages", "Home", "Features", "Plugins", "System", "Microsoft"];

    static readonly string[] TemplateSampleNames =
        [Templates.App, Templates.Module, Templates.Request, Templates.Command, Templates.Plugin];

    public static string Check(string name, string kind, IReadOnlyList<string>? forbiddenSuffixes = null) {
        if (!PascalCase().IsMatch(name)) {
            throw new UmpireException(
                $"The {kind} name '{name}' is not valid. Use a PascalCase C# identifier, for example CreateInvoice.");
        }
        if (name.StartsWith("Umpire", StringComparison.Ordinal) || NamesThatBreakTheBuild.Contains(name)) {
            throw new UmpireException(
                $"The {kind} name '{name}' is reserved. "
                + $"Names cannot start with Umpire or be one of: {string.Join(", ", NamesThatBreakTheBuild)}.");
        }
        foreach (var sample in TemplateSampleNames) {
            if (name.Contains(sample, StringComparison.Ordinal)) {
                throw new UmpireException(
                    $"The {kind} name '{name}' contains the template name {sample}. Use a different name.");
            }
        }
        foreach (var suffix in forbiddenSuffixes ?? []) {
            if (name.EndsWith(suffix, StringComparison.Ordinal)) {
                throw new UmpireException(
                    $"The {kind} name '{name}' ends with {suffix}. "
                    + $"Give the name without the suffix, for example {name[..^suffix.Length]}.");
            }
        }
        return name;
    }

    public static string Kebab(string name) {
        var text = new StringBuilder();
        for (var i = 0; i < name.Length; i++) {
            var c = name[i];
            var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
            if (i > 0 && char.IsUpper(c) && (!char.IsUpper(name[i - 1]) || nextIsLower)) {
                text.Append('-');
            }
            text.Append(char.ToLowerInvariant(c));
        }
        return text.ToString();
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$")]
    private static partial Regex PascalCase();
}

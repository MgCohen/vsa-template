using System.Text;
using System.Text.RegularExpressions;

namespace VSA.Cli.Scaffolding;

static partial class Names {
    // A name must be a PascalCase C# identifier. Some names collide with the framework or the app.
    public static string Check(string name, string kind) {
        if (!PascalCase().IsMatch(name)) {
            throw new VsaException($"The {kind} name '{name}' is not valid. Use a PascalCase C# identifier, for example CreateInvoice.");
        }
        if (name.StartsWith("VSA", StringComparison.Ordinal) || name == "App") {
            throw new VsaException($"The {kind} name '{name}' is reserved. Names cannot be App or start with VSA.");
        }
        foreach (var suffix in new[] { "Request", "Response", "Handler" }) {
            if (kind == "request" && name.EndsWith(suffix, StringComparison.Ordinal)) {
                throw new VsaException($"The request name '{name}' ends with {suffix}. Give the name without the suffix, for example {name[..^suffix.Length]}.");
            }
        }
        return name;
    }

    // CreateInvoice gives create-invoice, HTTPServer gives http-server. Routes use this form.
    public static string Kebab(string name) {
        var text = new StringBuilder();
        for (var i = 0; i < name.Length; i++) {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])))) {
                text.Append('-');
            }
            text.Append(char.ToLowerInvariant(c));
        }
        return text.ToString();
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$")]
    private static partial Regex PascalCase();
}

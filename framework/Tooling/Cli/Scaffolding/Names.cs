using System.Text;
using System.Text.RegularExpressions;

namespace VSA.Cli.Scaffolding;

static partial class Names {
    // Names that break the build of the app:
    //   App, Views, Pages, Home   the app folders and namespaces (CS0138 in App/Views/Pages)
    //   Modules                   the root folder of the modules, where VSA.Sdk finds the layer of a project
    //   System, Microsoft         a namespace with this name hides the .NET namespaces
    static readonly string[] Reserved = ["App", "Views", "Pages", "Home", "Modules", "System", "Microsoft"];

    // The tool adds these suffixes to the name of a request or a command.
    static readonly Dictionary<string, string[]> Suffixes = new() {
        ["request"] = ["Request", "Response", "Handler"],
        ["command"] = ["Command", "Result", "Handler"],
    };

    // A name must be a PascalCase C# identifier. Some names collide with the framework or the app.
    public static string Check(string name, string kind) {
        if (!PascalCase().IsMatch(name)) {
            throw new VsaException($"The {kind} name '{name}' is not valid. Use a PascalCase C# identifier, for example CreateInvoice.");
        }
        if (name.StartsWith("VSA", StringComparison.Ordinal) || Reserved.Contains(name)) {
            throw new VsaException($"The {kind} name '{name}' is reserved. Names cannot start with VSA or be one of: {string.Join(", ", Reserved)}.");
        }
        // The tool replaces the sample names in the templates. A name that contains one is replaced two times.
        foreach (var sample in new[] { Templates.App, Templates.Module, Templates.Request, Templates.Command }) {
            if (name.Contains(sample, StringComparison.Ordinal)) {
                throw new VsaException($"The {kind} name '{name}' contains the template name {sample}. Use a different name.");
            }
        }
        foreach (var suffix in Suffixes.GetValueOrDefault(kind, [])) {
            if (name.EndsWith(suffix, StringComparison.Ordinal)) {
                throw new VsaException($"The {kind} name '{name}' ends with {suffix}. Give the name without the suffix, for example {name[..^suffix.Length]}.");
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

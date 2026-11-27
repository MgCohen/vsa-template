namespace Umpire.Cli.Features.Scaffold.Domain;

using Umpire.Cli.Plugins.Commands.Domain.Shared;

sealed record AddKind(
    string Noun,
    string Shown,
    TemplateGroup Template,
    string SampleName,
    string? Folder = null,
    bool IsModule = false,
    IReadOnlyList<string>? Suffixes = null,
    IReadOnlyList<string>? BaseNames = null,
    Func<string, string>? NextStep = null) {
    const string SameNamespace =
        "A module and a plugin module with the same name have the same namespace. Use a different name.";

    public static readonly AddKind Module = new(
        "module",
        "module",
        TemplateGroup.Module,
        Templates.Module,
        Folder: "App/Features",
        IsModule: true,
        NextStep: name => $"The page of the module is /{Names.Kebab(name)}.");

    public static readonly AddKind Plugin = new(
        "plugin",
        "plugin module",
        TemplateGroup.Plugin,
        Templates.Plugin,
        Folder: "App/Plugins",
        IsModule: true,
        NextStep: name => $"Derive a module command from {name}Step<TResult>.");

    public static readonly AddKind Pack = new(
        "pack",
        "pack",
        TemplateGroup.Pack,
        Templates.Pack,
        Folder: "App/Packs",
        BaseNames: ["Vsa"],
        NextStep: name => $"Change the app class to App<{name}Kernel, WebHost>, then add parts and rules to "
            + $"{name}Architecture. If the kernel gets its own PackageReference items, add "
            + $"<ProjectReference Include=\"App/Packs/{name}/Kernel/{name}.Kernel.csproj\" /> to the root project. "
            + "See docs/pack-variants.md.");

    public static readonly AddKind Request = new(
        "request",
        "request",
        TemplateGroup.Request,
        Templates.Request,
        Suffixes: ["Request", "Response", "Handler"]);

    public static readonly AddKind Command = new(
        "command",
        "command",
        TemplateGroup.Command,
        Templates.Command,
        Suffixes: ["Command", "Result", "Handler"]);

    public static IReadOnlyList<AddKind> All { get; } = [Module, Plugin, Pack, Request, Command];

    public bool InModule => Folder is null;

    public void EnsureFree(string block, string name) {
        if (Folder is null) {
            return;
        }
        if (name == RootProject.Name(block)) {
            throw new UmpireException($"The {Shown} name {name} is the name of the app. "
                + $"A {Shown} with the app name makes usings that do not compile (CS0234). Use a different name.");
        }
        if (BaseNames?.Contains(name) == true) {
            throw new UmpireException($"The {Shown} name {name} is the name of a base {Shown}. "
                + $"The architecture {name}Architecture would inherit itself (CS0146). Use a different name.");
        }
        if (Directory.Exists(Path.Combine(block, Folder, name))) {
            throw new UmpireException($"The {Shown} {name} exists: {Folder}/{name}. Use a different name.");
        }
        foreach (var other in All.Where(kind => IsModule && kind.IsModule && kind != this)) {
            if (Directory.Exists(Path.Combine(block, other.Folder!, name))) {
                throw new UmpireException(
                    $"The {other.Shown} {name} exists: {other.Folder}/{name}. {SameNamespace}");
            }
        }
    }
}

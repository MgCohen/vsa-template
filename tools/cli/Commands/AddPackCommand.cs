namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class AddPackCommand : AddCommand {
    const string Packs = "Packs";
    static readonly string[] NamesOfBasePacks = ["Vsa"];

    public override IReadOnlyList<string> Words => ["add", "pack"];

    public override string Summary => "Adds a pack variant: a pack class and an architecture that inherits VsaArchitecture.";

    protected override string Noun => "pack";

    protected override TemplateGroup Group => TemplateGroup.Pack;

    protected override void CheckFree(string block, string name) {
        if (name == RootProject.Name(block)) {
            throw new UmpireException($"The pack name {name} is the name of the app. "
                + "A pack with the app name makes usings that do not compile (CS0234). Use a different name.");
        }
        if (NamesOfBasePacks.Contains(name)) {
            throw new UmpireException($"The pack name {name} is the name of a base pack. "
                + $"The architecture {name}Architecture would inherit itself (CS0146). Use a different name.");
        }
        if (Directory.Exists(Path.Combine(block, Packs, name))) {
            throw new UmpireException($"The pack {name} exists: {Packs}/{name}. Use a different name.");
        }
    }

    protected override IReadOnlyDictionary<string, string> TemplateNames(
        string block,
        string name,
        CommandInput input) =>
        new Dictionary<string, string> { [Templates.Pack] = name };

    protected override string Heading(string name, IReadOnlyDictionary<string, string> names) =>
        $"Adding the pack {name}:";

    protected override string NextStep(string name) =>
        $"Change the app class to App<{name}<WebVsaHost>>, then add parts and rules to {name}Architecture. See docs/pack-variants.md.";
}

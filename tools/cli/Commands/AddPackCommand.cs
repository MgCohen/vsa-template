namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class AddPackCommand : AddCommand {
    const string Packs = "Packs";

    public override IReadOnlyList<string> Words => ["add", "pack"];

    public override string Summary => "Adds a pack variant: a pack class and an architecture that inherits VsaArchitecture.";

    protected override string Noun => "pack";

    protected override Templates.Kind Kind => Templates.Kind.Pack;

    protected override void CheckFree(string block, string name) {
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

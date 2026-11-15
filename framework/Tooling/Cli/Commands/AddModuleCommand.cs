using VSA.Cli.Commands.Bases;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

sealed class AddModuleCommand : AddCommand {
    public override IReadOnlyList<string> Words => ["add", "module"];

    public override string Summary => "Adds a module with all layers and a starter page.";

    protected override string Noun => "module";

    protected override Templates.Kind Kind => Templates.Kind.Module;

    protected override void CheckFree(string root, string name) => ModuleFolder.EnsureFree(root, name);

    protected override IReadOnlyDictionary<string, string> OwnNames(string root, string name, CommandInput input) =>
        new Dictionary<string, string> { [Templates.Module] = name };

    protected override string Heading(string name, IReadOnlyDictionary<string, string> names) =>
        $"Adding the module {name}:";

    protected override string NextStep(string name) => $"The page of the module is /{Names.Kebab(name)}.";
}

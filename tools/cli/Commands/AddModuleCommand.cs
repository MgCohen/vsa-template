namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class AddModuleCommand : AddCommand {
    public override IReadOnlyList<string> Words => ["add", "module"];

    public override string Summary => "Adds a module with all layers and a starter page.";

    protected override string Noun => "module";

    protected override Templates.Kind Kind => Templates.Kind.Module;

    protected override void CheckFree(string block, string name) =>
        ModuleFolder.EnsureFree(block, name, ModuleKind.Module);

    protected override IReadOnlyDictionary<string, string> TemplateNames(
        string block,
        string name,
        CommandInput input) =>
        new Dictionary<string, string> { [Templates.Module] = name };

    protected override string Heading(string name, IReadOnlyDictionary<string, string> names) =>
        $"Adding the module {name}:";

    protected override string NextStep(string name) => $"The page of the module is /{Names.Kebab(name)}.";
}

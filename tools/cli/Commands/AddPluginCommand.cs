namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class AddPluginCommand : AddCommand {
    public override IReadOnlyList<string> Words => ["add", "plugin"];

    public override string Summary => "Adds a plugin module: a base of the same layers in each module.";

    protected override string Noun => "plugin";

    protected override TemplateGroup Group => TemplateGroup.Plugin;

    protected override void CheckFree(string block, string name) =>
        ModuleFolder.EnsureFree(block, name, ModuleKind.Plugin);

    protected override IReadOnlyDictionary<string, string> TemplateNames(
        string block,
        string name,
        CommandInput input) =>
        new Dictionary<string, string> { [Templates.Plugin] = name };

    protected override string Heading(string name, IReadOnlyDictionary<string, string> names) =>
        $"Adding the plugin module {name}:";

    protected override string NextStep(string name) => $"Derive a module command from {name}Step<TResult>.";
}

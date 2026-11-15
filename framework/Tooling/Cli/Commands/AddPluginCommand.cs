using VSA.Cli.Commands.Bases;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

sealed class AddPluginCommand : AddCommand {
    public override IReadOnlyList<string> Words => ["add", "plugin"];

    public override string Summary => "Adds a plugin module: a base of the same layers in each module.";

    protected override string Noun => "plugin";

    protected override Templates.Kind Kind => Templates.Kind.Plugin;

    protected override void CheckFree(string root, string name) => ModuleFolder.EnsureFree(root, name);

    protected override IReadOnlyDictionary<string, string> OwnNames(string root, string name, CommandInput input) =>
        new Dictionary<string, string> { [Templates.Plugin] = name };

    protected override string Heading(string name, IReadOnlyDictionary<string, string> names) =>
        $"Adding the plugin module {name}:";

    protected override string NextStep(string name) => $"Derive a module command from {name}Step<TResult>.";
}

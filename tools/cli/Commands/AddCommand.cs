namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class AddCommand(AddKind kind) : AppCommand {
    static readonly CommandOption Module = new(["-m", "--module"], "Module", Required: true);

    public AddKind Kind => kind;

    public override IReadOnlyList<string> Words => ["add", kind.Noun];

    public override string Summary => kind.Summary;

    public override bool TakesName => true;

    public override IReadOnlyList<CommandOption> Options => kind.InModule ? [Module] : [];

    protected override void CheckName(CommandInput input) => Names.Check(input.Name, kind.Noun, kind.Suffixes);

    protected override int Run(string block, CommandInput input) {
        var name = input.Name;
        kind.EnsureFree(block, name);
        var names = new Dictionary<string, string> {
            [kind.SampleName] = name,
            [Templates.App] = RootProject.Name(block),
        };
        var module = kind.InModule ? ModuleFolder.Find(block, input.Single(Module)!) : null;
        if (module is not null) {
            names[Templates.Module] = module;
        }
        var inModule = module is null ? "" : $" to the module {module}";
        Console.WriteLine($"Adding the {kind.Shown} {name}{inModule}:");
        FileWriter.WriteAllOrNone(block, Templates.Files(kind.Template, names, ToolVersion.Current));
        if (kind.NextStep?.Invoke(name) is string next) {
            Console.WriteLine(next);
        }
        return 0;
    }
}

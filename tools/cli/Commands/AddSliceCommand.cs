namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class AddSliceCommand(SliceKind kind) : AddCommand {
    static readonly CommandOption Module = new(["-m", "--module"], "Module", Required: true);

    public SliceKind SliceKind => kind;

    public override IReadOnlyList<string> Words => ["add", kind.Noun];

    public override string Summary => $"Adds {kind.Parts}.";

    public override IReadOnlyList<CommandOption> Options => [Module];

    protected override string Noun => kind.Noun;

    protected override Templates.Kind Kind => kind.Template;

    protected override IReadOnlyList<string> ForbiddenSuffixes => kind.Suffixes;

    protected override IReadOnlyDictionary<string, string> TemplateNames(
        string block,
        string name,
        CommandInput input) =>
        new Dictionary<string, string> {
            [kind.SampleName] = name,
            [Templates.Module] = ModuleFolder.Find(block, input.Single(Module)!),
        };

    protected override string Heading(string name, IReadOnlyDictionary<string, string> names) =>
        $"Adding the {kind.Noun} {name} to the module {names[Templates.Module]}:";
}

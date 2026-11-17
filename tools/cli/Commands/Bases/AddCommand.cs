namespace Umpire.Cli.Commands.Bases;

using Umpire.Cli.Scaffolding;

abstract class AddCommand : AppCommand {
    public override bool TakesName => true;

    protected abstract string Noun { get; }

    protected abstract TemplateGroup Group { get; }

    protected virtual IReadOnlyList<string> ForbiddenSuffixes => [];

    protected sealed override void CheckName(CommandInput input) => Names.Check(input.Name, Noun, ForbiddenSuffixes);

    protected virtual void CheckFree(string block, string name) { }

    protected abstract IReadOnlyDictionary<string, string> TemplateNames(
        string block,
        string name,
        CommandInput input);

    protected abstract string Heading(string name, IReadOnlyDictionary<string, string> names);

    protected virtual string? NextStep(string name) => null;

    protected sealed override int Run(string block, CommandInput input) {
        var name = input.Name;
        CheckFree(block, name);
        var names = new Dictionary<string, string>(TemplateNames(block, name, input));
        names[Templates.App] = RootProject.Name(block);
        Console.WriteLine(Heading(name, names));
        FileWriter.WriteAllOrNone(block, Templates.Files(Group, names, ToolVersion.Current));
        if (NextStep(name) is string next) {
            Console.WriteLine(next);
        }
        return 0;
    }
}

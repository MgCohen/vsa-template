namespace Umpire.Cli.Commands.Bases;

using Umpire.Cli.Scaffolding;

abstract class AddCommand : AppCommand {
    public override bool TakesName => true;

    protected abstract string Noun { get; }

    protected abstract Templates.Kind Kind { get; }

    protected virtual IReadOnlyList<string> ForbiddenSuffixes => [];

    protected sealed override void CheckName(CommandInput input) => Names.Check(input.Name, Noun, ForbiddenSuffixes);

    protected virtual void CheckFree(string root, string name) { }

    protected abstract IReadOnlyDictionary<string, string> TemplateNames(
        string root,
        string name,
        CommandInput input);

    protected abstract string Heading(string name, IReadOnlyDictionary<string, string> names);

    protected virtual string? NextStep(string name) => null;

    protected sealed override int Run(string root, CommandInput input) {
        var name = input.Name;
        CheckFree(root, name);
        var names = new Dictionary<string, string>(TemplateNames(root, name, input));
        names[Templates.App] = RootProject.Name(root);
        Console.WriteLine(Heading(name, names));
        FileWriter.WriteAllOrNone(root, Templates.Files(Kind, names, ToolVersion.Current));
        if (NextStep(name) is string next) {
            Console.WriteLine(next);
        }
        return 0;
    }
}

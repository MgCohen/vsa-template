using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands.Bases;

abstract class AddCommand : AppCommand {
    public override bool TakesName => true;

    protected abstract string Noun { get; }

    protected abstract Templates.Kind Kind { get; }

    protected virtual IReadOnlyList<string> ForbiddenSuffixes => [];

    protected virtual void CheckFree(string root, string name) { }

    protected abstract IReadOnlyDictionary<string, string> OwnNames(string root, string name, CommandInput input);

    protected abstract string Heading(string name, IReadOnlyDictionary<string, string> names);

    protected virtual string? NextStep(string name) => null;

    protected sealed override int Run(string root, CommandInput input) {
        var name = Names.Check(input.Name, Noun, ForbiddenSuffixes);
        CheckFree(root, name);
        var names = TemplateNames(root, name, input);
        Console.WriteLine(Heading(name, names));
        FileWriter.WriteAllOrNone(root,Templates.Files(Kind, names, ToolVersion.Current));
        if (NextStep(name) is string next) {
            Console.WriteLine(next);
        }
        return 0;
    }

    IReadOnlyDictionary<string, string> TemplateNames(string root, string name, CommandInput input) =>
        new Dictionary<string, string>(OwnNames(root, name, input));
}

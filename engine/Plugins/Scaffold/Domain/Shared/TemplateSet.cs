namespace Scaffold.Domain.Shared;

using Umpire.Architecture;

public sealed record TemplateSet(string Kind, string Group, IReadOnlyList<Binding> Types) {
    public Binding First => Types[0];

    public string Token => First.Values[Group];

    public string Folder => Path.GetDirectoryName(First.Source!.File)!;
}

namespace CheckModel.Tests;

using Umpire.CheckModel;

static class Samples {
    public static readonly PartOptions Deny = new(Permission.Deny, Boundary.Open, false, Depth.WithChildren, Access.Public, false);
    public static readonly TypeName Ui = Concept("Ui");
    public static readonly TypeName Data = Concept("Data");

    public static Definition Layers => new(
        Definition.CurrentFormat, "Layers", [],
        [Part(Ui, "Ui"), Part(Data, "Data")],
        [new UseRule(false, [Entry(Ui)], Permission.Allow, [Entry(Data)], [new UsePair(0, 1, Access.Public, [], [])])],
        [], [], [], []);

    public static BlockSettings Settings(params string[] projects) => BlockSettings.Of("../..", projects) with {
        DefinitionSources = [new FileMark("NoSuchSource.cs", "", 0)],
    };

    public static TypeName Concept(string name) => new($"Sample.Definition.{name}", "Sample.Definition");

    public static TypeName Name(string name) => new($"Sample.{name}", "Sample");

    public static RuleEntry Entry(TypeName concept) => new(concept, []);

    public static PartDefinition Part(TypeName concept, string folder, params PartDefinition[] children) =>
        new(concept, folder, false, false, false, false, false, Deny, [], children, folder);

    public static TypeFacts Type(string name, params ReferenceFact[] references) => new(
        Name(name), new TypeLocation("Sample", 3, 14),
        new TypeContent(
            TypeKind.Class, MemberAccess.Public, false, false, true, false, false, false, false, [], [], [], [Name(name)], [], [], [], []),
        references);

    public static ReferenceFact Use(string name, int line) => new(Name(name), ReferenceKind.Create, line, 9, "Run()", ".ctor");

    public static FileFacts File(string path, params TypeFacts[] types) => new(path, "", 0, false, types);
}

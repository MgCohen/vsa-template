namespace VSA.Architecture;

public sealed record DataConcept(Concept Inner, ConceptData Data) : Concept {
    internal override Type Identity => Inner.Identity;

    public override string Folder => Data.Folder ?? Inner.Folder;

    public override string? GroupFolder => Data.GroupFolder ?? Inner.GroupFolder;

    protected internal override void Configure(ConceptOptions options) {
        Inner.Configure(options);
        options.Depth = Data.Depth ?? options.Depth;
        options.Access = Data.Access ?? options.Access;
    }

    protected internal override void Define(Part part) => Inner.Define(part);

    protected internal override void Rules(ConceptRules rules) => Inner.Rules(rules);
}

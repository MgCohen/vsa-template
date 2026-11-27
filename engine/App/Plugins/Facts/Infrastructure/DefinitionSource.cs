namespace Facts.Infrastructure;

public sealed class DefinitionSource : IFactSource<DefinitionQuery, AppLoader> {
    public AppLoader Read(DefinitionQuery query) => new(query.Bin, query.References);
}

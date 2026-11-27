namespace UmpireEngine;

using Facts.Infrastructure;

public sealed class DefinitionSource : IFactSource<DefinitionQuery, IDefinitionLoader> {
    public IDefinitionLoader Read(DefinitionQuery query) => new AppLoader(query.Bin, query.References);
}

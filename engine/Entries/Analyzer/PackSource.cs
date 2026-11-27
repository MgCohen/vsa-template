namespace UmpireEngine;

using Facts.Infrastructure;

sealed class PackSource : IFactSource<PackQuery, bool> {
    public bool Read(PackQuery query) =>
        PackSelection.Selects(CompileProjects.Of(query.Project), query.Architecture, query.PackNamespace);
}

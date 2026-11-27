namespace Facts.Infrastructure;

public sealed class TypeFactSource : IFactSource<TypeFactsQuery, LoadedApp> {
    public LoadedApp Read(TypeFactsQuery query) =>
        AppReader.Read(query.Dlls, query.Sources, query.BlockFolder, query.Architecture, query.IgnoredFolders);
}

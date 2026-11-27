namespace Facts.Infrastructure;

using Facts.Domain.Shared.Dll;

public sealed class DllSource : IFactSource<DllQuery, IReadOnlyList<DllFacts>> {
    public IReadOnlyList<DllFacts> Read(DllQuery query) {
        if (query.Dlls.Count == 0) {
            return [];
        }
        using var reader = new DllReader(query.Dlls.Concat(query.Resolve), query.SourceRoot);
        return [.. query.Dlls.Select(reader.Read).OfType<DllFacts>()];
    }
}

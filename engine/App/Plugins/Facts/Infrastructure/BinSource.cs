namespace Facts.Infrastructure;

public sealed class BinSource : IFactSource<BinQuery, BinFacts> {
    public BinFacts Read(BinQuery query) {
        var reader = new BinReader(query.Bin, query.Options);
        try {
            return new BinFacts(reader);
        } catch {
            reader.Dispose();
            throw;
        }
    }
}

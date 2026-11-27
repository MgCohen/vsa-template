namespace Facts.Infrastructure;

using Facts.Domain.Shared;

public sealed class BinFacts : IDisposable {
    readonly BinReader reader;

    public BinFacts(BinReader reader) {
        this.reader = reader;
        Index = reader.Index();
    }

    public BinIndex Index { get; }

    public BlockEntry Check(BlockEntry entry) => reader.Check(entry);

    public void Dispose() => reader.Dispose();
}

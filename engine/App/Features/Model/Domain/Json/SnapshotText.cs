namespace Umpire.CheckModel;

public static class SnapshotText {
    public static Snapshot Read(string text) {
        var snapshot = JsonReader.Read<Snapshot>(text);
        return snapshot.Format == Snapshot.CurrentFormat
            ? snapshot
            : throw new FormatException($"The snapshot has the format {snapshot.Format}. This tool reads {Snapshot.CurrentFormat}.");
    }
}

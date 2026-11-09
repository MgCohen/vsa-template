namespace Arena.Slices.Duel;

public sealed class ArenaLog {
    readonly List<string> lines = [];

    public IReadOnlyList<string> Lines {
        get {
            lock (lines) {
                return [.. lines];
            }
        }
    }

    public void Add(string line) {
        lock (lines) {
            lines.Add(line);
        }
    }
}

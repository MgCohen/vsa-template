namespace VSA.Architecture.Versions;

public sealed class VersionSlot(
    string file, string package, string? version, int? line = null, Action<string>? write = null) {
    public string File => file;

    public string Package => package;

    public string? Version { get; private set; } = version;

    public int? Line => line;

    public bool Set(string value) {
        if (write is null || Version is null || Version == value) {
            return false;
        }
        write(value);
        Version = value;
        return true;
    }
}

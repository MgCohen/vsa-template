namespace Umpire.Engine.Versions;

public sealed class VersionFile(string file, IReadOnlyList<VersionSlot> slots, Func<string> content) {
    public string File => file;

    public IReadOnlyList<VersionSlot> Slots => slots;

    public string Content => content();

    public bool SetVersion(string version) {
        var changed = false;
        foreach (var slot in slots) {
            changed |= slot.Set(version);
        }
        return changed;
    }
}

namespace VSA.Architecture.Versions;

public sealed class VersionFile(string file, IReadOnlyList<VersionSlot> slots, Action save) {
    public string File => file;

    public IReadOnlyList<VersionSlot> Slots => slots;

    public bool SetVersion(string version) {
        var changed = false;
        foreach (var slot in slots) {
            changed |= slot.Set(version);
        }
        if (changed) {
            save();
        }
        return changed;
    }
}

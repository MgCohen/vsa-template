using Umpire.Engine.Versions;
using Umpire.Cli.Commands.Bases;

namespace Umpire.Cli.Commands;

sealed class DoctorCommand : AppCommand {
    public override IReadOnlyList<string> Words => ["doctor"];

    public override string Summary => "Makes sure that the tool, Umpire.Sdk and the packages have one version.";

    internal static IReadOnlyList<VersionSlot> Slots(string root) {
        var tool = new VersionSlot("this tool", "Umpire.Cli", ToolVersion.Current);
        return [tool, .. VersionSlots.InApp(root).SelectMany(file => file.Slots)];
    }

    protected override int Run(string root, CommandInput input) {
        var expected = ToolVersion.Current;
        var slots = Slots(root);
        var width = slots.Max(slot => slot.File.Length);
        foreach (var slot in slots) {
            Console.WriteLine($"  {slot.File.PadRight(width)}  {slot.Package,-24} {VersionCheck.Shown(slot)}");
        }
        var mismatches = VersionCheck.Mismatches(slots, expected);
        if (mismatches is []) {
            Console.WriteLine($"OK: all use Umpire {expected}.");
            return 0;
        }
        throw new UmpireException(VersionCheck.Message(mismatches, expected));
    }
}

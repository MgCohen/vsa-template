namespace Umpire.Cli.Commands;

using Umpire.Engine.Versions;
using Umpire.Cli.Commands.Bases;

sealed class DoctorCommand : AppCommand {
    public override IReadOnlyList<string> Words => ["doctor"];

    public override string Summary => "Makes sure that the tool, the Umpire SDKs and the Umpire packages of the app have one version.";

    internal static IReadOnlyList<VersionSlot> Slots(string block) {
        var tool = new VersionSlot("this tool", "Umpire.Cli", ToolVersion.Current);
        return [tool, .. VersionSlots.InApp(block).SelectMany(file => file.Slots)];
    }

    protected override int Run(string block, CommandInput input) {
        var expected = ToolVersion.Current;
        var slots = Slots(block);
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

namespace Umpire.Cli.Features.Versions.Slices.Doctor;

using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;
using Umpire.Engine.Versions;

sealed class DoctorHandler : ToolHandler<DoctorRequest, DoctorResponse> {
    internal static IReadOnlyList<VersionSlot> Slots(string block) {
        var tool = new VersionSlot("this tool", "Umpire.Cli", ToolVersion.Current);
        return [tool, .. VersionSlots.InApp(block).SelectMany(file => file.Slots)];
    }

    protected override Task<DoctorResponse> Run(DoctorRequest request, CancellationToken ct) {
        var block = RootProject.AppOfCurrentFolder();
        var expected = ToolVersion.Current;
        var slots = Slots(block);
        var width = slots.Max(slot => slot.File.Length);
        foreach (var slot in slots) {
            Console.WriteLine($"  {slot.File.PadRight(width)}  {slot.Package,-24} {VersionCheck.Shown(slot)}");
        }
        var removed = VersionSlots.RemovedSdks(block);
        if (removed is not []) {
            throw new UmpireException(
                "Change these by hand: " + string.Join("; ", removed) + ". Then run 'umpire doctor' again.");
        }
        var mismatches = VersionCheck.Mismatches(slots, expected);
        if (mismatches is []) {
            Console.WriteLine($"OK: all use Umpire {expected}.");
            return Task.FromResult(new DoctorResponse(0));
        }
        throw new UmpireException(VersionCheck.Message(mismatches, expected));
    }
}

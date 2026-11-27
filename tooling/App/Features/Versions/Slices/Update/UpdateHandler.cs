namespace Umpire.Cli.Features.Versions.Slices.Update;

using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;
using Umpire.Engine.Versions;

sealed class UpdateHandler : ToolHandler<UpdateRequest, UpdateResponse> {
    internal static IReadOnlyList<string> SetVersion(string app, string version) {
        var changed = new List<string>();
        foreach (var file in VersionSlots.InApp(app)) {
            if (file.SetVersion(version)) {
                changed.Add(file.File);
            }
        }
        return changed;
    }

    protected override Task<UpdateResponse> Run(UpdateRequest request, CancellationToken ct) {
        var block = RootProject.AppOfCurrentFolder();
        var target = request.Version ?? ToolVersion.Current;
        var removed = VersionSlots.RemovedSdks(block);
        if (removed is not []) {
            throw new UmpireException(
                "Change these by hand: " + string.Join("; ", removed) + ". Then run 'umpire update' again.");
        }
        var changed = SetVersion(block, target);
        Console.WriteLine($"Umpire {target}:");
        foreach (var file in changed) {
            Console.WriteLine($"  {file}");
        }
        Console.WriteLine(changed is []
            ? "The files already use this version."
            : $"Changed {changed.Count} files. Next: dotnet build");
        return Task.FromResult(new UpdateResponse(0));
    }
}

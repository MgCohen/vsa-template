using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace VSA.Sdk.Tasks;

// Gives the slice files of one layer, for the Compile items of that layer project (VSA.Layers.targets, _VsaRouteSlices).
// Each routed project reports all routing errors as VSA0005, so each project that the error touches fails.
public sealed class VsaRouteSliceFiles : Microsoft.Build.Utilities.Task {
    [Required]
    public string SlicesDir { get; set; } = "";

    public ITaskItem[] Files { get; set; } = [];

    // The layer folders of the Shared modules. The router reads their types as bases, and does not route them.
    public ITaskItem[] KnownFiles { get; set; } = [];

    [Required]
    public string Layer { get; set; } = "";

    [Output]
    public ITaskItem[] LayerFiles { get; set; } = [];

    public override bool Execute() {
        var sources = Files.Select(f => f.GetMetadata("FullPath")).Select(path => new SliceSource(path, File.ReadAllText(path)));
        var known = KnownFiles.Select(f => f.GetMetadata("FullPath")).Select(path => new SliceSource(path, File.ReadAllText(path)));
        var routing = SliceRouter.Route(SlicesDir, sources, known);
        foreach (var error in routing.Errors) {
            Log.LogError(null, "VSA0005", null, error.File, 0, 0, 0, 0, error.Message);
        }
        var root = Path.GetFullPath(SlicesDir);
        LayerFiles = [.. routing.Layers.Where(pair => pair.Value == Layer).Select(pair => {
            var item = new TaskItem(pair.Key);
            item.SetMetadata("Link", "Slices/" + SliceRouter.Relative(root, pair.Key));
            return (ITaskItem)item;
        })];
        return !Log.HasLoggedErrors;
    }
}

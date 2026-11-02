using System.Text.RegularExpressions;
using System.Xml.Linq;
using VSA.Testing.Core;

namespace Rules.Tests;

// The layer map is in two places: Layers.cs (C#, for the tests) and VSA.Layers.targets (MSBuild, for the build).
// This test fails if the two maps are different:
//   - each reference that the Sdk adds must have an edge in Layers.cs, and
//   - each edge in Layers.cs to a module layer must be a reference that the Sdk adds.
public sealed partial class SdkLayerMapTests {
    // The layers that have module projects. Infrastructure.EfCore is framework only. Views and Host are app layers.
    static readonly string[] ModuleLayers = ["Domain", "Infrastructure", "Contracts.Requests", "Contracts.Events", "Handlers.Requests", "Handlers.Events", "Presenters", "Module"];

    sealed record SdkLayer(string Name, HashSet<string> Own, HashSet<string> AnyModule, HashSet<string> Bases);

    [Test]
    public void Each_module_layer_of_the_map_is_in_the_Sdk() =>
        Assert.That(Sdk().Keys, Is.SupersetOf(ModuleLayers));

    [Test]
    public void Each_Sdk_reference_has_an_edge_in_the_map() {
        List<string> missing = [];
        foreach (var sdk in Sdk().Values) {
            var layer = Layers.Find(sdk.Name);
            if (layer is null) {
                missing.Add($"{sdk.Name}: not in Layers.cs");
                continue;
            }
            var edges = layer.References.Concat(layer.Exposed).ToList();
            missing.AddRange(sdk.Own.Where(t => !edges.Any(e => e.Target == t && e.Edge != Edge.BaseOnly)).Select(t => $"{sdk.Name} -> {t} (own module)"));
            missing.AddRange(sdk.AnyModule.Where(t => !edges.Any(e => e.Target == t && e.AnyModule)).Select(t => $"{sdk.Name} -> {t} (any module)"));
            missing.AddRange(sdk.Bases.Where(t => t != sdk.Name && !edges.Any(e => e.Target == t)).Select(t => $"{sdk.Name} -> VSA.{t} (base)"));
        }
        Assert.That(missing, Is.Empty, "VSA.Layers.targets adds references that Layers.cs does not permit.");
    }

    [Test]
    public void Each_module_edge_of_the_map_is_an_Sdk_reference() {
        var sdk = Sdk();
        List<string> missing = [];
        foreach (var layer in Layers.All.Where(l => sdk.ContainsKey(l.Name))) {
            foreach (var edge in layer.References.Where(e => e.Edge != Edge.BaseOnly && ModuleLayers.Contains(e.Target))) {
                var found = edge.AnyModule ? sdk[layer.Name].AnyModule : sdk[layer.Name].Own;
                if (!found.Contains(edge.Target)) {
                    missing.Add($"{layer.Name} -> {edge.Target}{(edge.AnyModule ? " (any module)" : "")}");
                }
            }
        }
        Assert.That(missing, Is.Empty, "Layers.cs has edges that VSA.Layers.targets does not add.");
    }

    // Reads the item groups with Condition="'$(VsaLayer)' == '<folder>'" in VSA.Layers.targets.
    static Dictionary<string, SdkLayer> Sdk() {
        var targets = XDocument.Load(Path.Combine(Repo(), "framework", "Sdk", "Sdk", "VSA.Layers.targets"));
        var layers = new Dictionary<string, SdkLayer>();
        foreach (var group in targets.Root!.Elements().Where(e => e.Name.LocalName == "ItemGroup")) {
            if (LayerCondition().Match((string?)group.Attribute("Condition") ?? "") is not { Success: true } condition) {
                continue;
            }
            var name = condition.Groups[1].Value.Replace('/', '.');
            var layer = layers.TryGetValue(name, out var found) ? found : layers[name] = new(name, [], [], []);
            foreach (var item in group.Elements()) {
                foreach (var include in ((string?)item.Attribute("Include") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)) {
                    Read(item.Name.LocalName, include, layer);
                }
            }
        }
        return layers;
    }

    static void Read(string item, string include, SdkLayer layer) {
        if (item == "_VsaFramework") {
            if (include.StartsWith("VSA.", StringComparison.Ordinal) && Layers.Find(include[4..]) is not null) {
                layer.Bases.Add(include[4..]);
            }
        } else if (include == "$(VsaModuleDir)*/*.csproj" || include == "$(VsaModuleDir)*/*/*.csproj") {
            // The module root: all layer projects of its own module.
            layer.Own.UnionWith(ModuleLayers.Where(l => l != "Module"));
        } else if (OwnModule().Match(include) is { Success: true } own) {
            layer.Own.Add(own.Groups[1].Value.Replace('/', '.'));
        } else if (AnyModule().Match(include) is { Success: true } any) {
            layer.AnyModule.Add(any.Groups[1].Value.Replace('/', '.'));
        }
    }

    static string Repo() {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent) {
            if (File.Exists(Path.Combine(dir.FullName, "VSA.slnx"))) {
                return dir.FullName;
            }
        }
        throw new InvalidOperationException("VSA.slnx not found above the test output folder.");
    }

    [GeneratedRegex(@"^'\$\(VsaLayer\)' == '([^']+)'$")]
    private static partial Regex LayerCondition();

    [GeneratedRegex(@"^\$\(VsaModuleDir\)(.+)/\*\.csproj$")]
    private static partial Regex OwnModule();

    [GeneratedRegex(@"^\$\(VsaModulesDir\)\*/(.+)/\*\.csproj$")]
    private static partial Regex AnyModule();
}

namespace BuiltApp.Infrastructure;

using System.Text.Json;
using System.Text.Json.Nodes;

public static class DepsFile {
    const string Project = "project";

    public static IReadOnlySet<string> ProjectAssemblies(string bin) {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = Directory.Exists(bin) ? Directory.GetFiles(bin, "*.deps.json") : [];
        foreach (var file in files) {
            names.UnionWith(ProjectAssembliesOf(file));
        }
        return names;
    }

    static IEnumerable<string> ProjectAssembliesOf(string file) {
        if (Json(file) is not { } deps || deps["libraries"] is not JsonObject libraries
            || deps["targets"] is not JsonObject targets) {
            yield break;
        }
        foreach (var target in targets.Select(t => t.Value).OfType<JsonObject>()) {
            foreach (var (key, library) in target) {
                var type = libraries[key]?["type"]?.GetValue<string>();
                if (type != Project || library?["runtime"] is not JsonObject runtime) {
                    continue;
                }
                foreach (var dll in runtime.Select(r => r.Key)) {
                    yield return Path.GetFileNameWithoutExtension(dll);
                }
            }
        }
    }

    static JsonNode? Json(string file) {
        try {
            var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };
            return JsonNode.Parse(File.ReadAllText(file), documentOptions: options);
        } catch (JsonException) {
            return null;
        }
    }
}

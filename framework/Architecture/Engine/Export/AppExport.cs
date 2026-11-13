using System.Globalization;
using ArchUnitNET.Domain;
using VSA.Architecture.Data;

namespace VSA.Architecture.Engine;

sealed class AppExport(ArchitectureMap architecture, DefinitionExport definition) {
    const string Error = ": error ";

    readonly Dictionary<string, InstanceData> instances = [];
    readonly Dictionary<Binding, TypeData> types = [];

    public AppData Data(string name, string root) {
        List<Binding> bindings = [.. architecture.Bindings.Where(b => !b.External)];
        foreach (var binding in bindings) {
            types[binding] = TypeOf(binding);
        }
        var byType = types.Keys.ToDictionary(b => b.Type);
        List<UseData> uses = [.. bindings.SelectMany(b => Uses(b, byType))];
        var byId = types.Values.ToDictionary(t => t.Id);
        List<DependencyData> dependencies = [.. uses
            .Where(u => byId[u.From].Instance != byId[u.To].Instance)
            .GroupBy(u => new { From = byId[u.From].Instance, To = byId[u.To].Instance })
            .Select(g => new DependencyData(g.Key.From, g.Key.To, g.Any(u => u.Access == "Deny") ? "Deny" : "Allow", [.. g]))];
        List<FindingData> findings = [.. bindings.SelectMany(b => architecture.Findings(b.Type).Select(f => FindingOf(f, types[b].Id)))];
        return new AppData(name, root, [.. instances.Values], [.. types.Values], dependencies, findings);
    }

    TypeData TypeOf(Binding binding) => new(
        $"t{types.Count}", binding.Type.Name, binding.Type.FullName, InstanceOf(binding.Part, binding.Values),
        Relative(binding.Source.File), binding.Source.Line, binding.FollowsUser);

    IEnumerable<UseData> Uses(Binding from, Dictionary<IType, Binding> byType) {
        foreach (var target in from.Type.Dependencies.Select(d => d.Target).Distinct()) {
            if (!byType.TryGetValue(target, out var to) || to == from) {
                continue;
            }
            var decision = Decide.For(architecture.Definition, from, to);
            var rule = decision.Rule is null ? null : definition.RuleIds[decision.Rule.Text];
            yield return new UseData(types[from].Id, types[to].Id, decision.Permission.ToString(), rule);
        }
    }

    string InstanceOf(Part part, IReadOnlyDictionary<string, string> values) {
        var path = values.Aggregate(part.Path, (p, value) => p.Replace($"{{{value.Key.ToLowerInvariant()}}}", value.Value));
        var id = $"{definition.PartIds[part]}@{path}";
        if (instances.ContainsKey(id)) {
            return id;
        }
        var parent = part.Parent is null ? null : InstanceOf(part.Parent, values);
        var own = part.Groups.Where(g => values.ContainsKey(g.Name)).ToDictionary(g => g.Name, g => values[g.Name]);
        instances[id] = new InstanceData(id, definition.PartIds[part], part.Name, parent, path, own);
        return id;
    }

    string Relative(string file) {
        var relative = Path.IsPathRooted(file) ? Path.GetRelativePath(architecture.AppRoot, file) : file;
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    static FindingData FindingOf(string text, string typeId) {
        var error = text.IndexOf(Error, StringComparison.Ordinal);
        var location = text[..error];
        var rest = text[(error + Error.Length)..];
        var colon = rest.IndexOf(':', StringComparison.Ordinal);
        var open = location.LastIndexOf('(');
        var file = open < 0 ? location : location[..open];
        var line = open < 0 ? 0 : int.Parse(location[(open + 1)..^1], CultureInfo.InvariantCulture);
        return new FindingData(rest[..colon], file, line, rest[(colon + 2)..], typeId);
    }
}

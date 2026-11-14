using ArchUnitNET.Domain;
using Viewer.Export.Model;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace Viewer.Export;

public sealed class AppExport(ArchitectureMap architecture, DefinitionExport definition)
{
    private readonly Dictionary<string, InstanceData> instances = [];
    private readonly Dictionary<Binding, TypeData> types = [];

    public AppData Data(string name, string root)
    {
        List<Binding> bindings = [.. architecture.Bindings.All.Where(b => !b.External)];
        foreach (Binding binding in bindings)
        {
            types[binding] = TypeOf(binding);
        }
        List<UseData> uses = [.. bindings.SelectMany(Uses)];
        Dictionary<string, TypeData> byId = types.Values.ToDictionary(t => t.Id);
        List<DependencyData> dependencies = [.. uses
            .Where(u => byId[u.From].Instance != byId[u.To].Instance)
            .GroupBy(u => (byId[u.From].Instance, byId[u.To].Instance))
            .Select(g => new DependencyData(
                g.Key.Item1, g.Key.Item2, g.Any(u => u.Access == "Deny") ? "Deny" : "Allow", [.. g]))];
        List<FindingData> findings = [.. bindings
            .SelectMany(b => Findings.Of(architecture, b.Type, types[b].Id, Relative))];
        return new AppData(name, root, [.. instances.Values], [.. types.Values], dependencies, findings);
    }

    private TypeData TypeOf(Binding binding)
    {
        string instance = InstanceOf(binding.Part, binding.Values);
        string file = Relative(binding.Source.File);
        string id = $"t{types.Count}";
        return new TypeData(
            id, binding.Type.Name, binding.Type.FullName, instance, file, binding.Source.Line, binding.FollowsUser);
    }

    private IEnumerable<UseData> Uses(Binding from)
    {
        Dictionary<IType, Binding> byType = types.Keys.ToDictionary(b => b.Type);
        foreach (IType target in from.Type.Dependencies.Select(d => d.Target).Distinct())
        {
            if (!byType.TryGetValue(target, out Binding? to) || to == from)
            {
                continue;
            }
            (Permission permission, PartRule? pair) = Decide.For(architecture.Definition, from, to);
            string? rule = pair is null ? null : definition.RuleIds[pair.Text];
            yield return new UseData(types[from].Id, types[to].Id, permission.ToString(), rule);
        }
    }

    private string InstanceOf(Part part, IReadOnlyDictionary<string, string> items)
    {
        string path = items.Aggregate(part.Path, (p, item) =>
            p.Replace($"{{{item.Key.ToLowerInvariant()}}}", item.Value, StringComparison.Ordinal));
        string id = $"{definition.PartIds[part]}@{path}";
        if (instances.ContainsKey(id))
        {
            return id;
        }
        string? parent = part.Parent is null ? null : InstanceOf(part.Parent, items);
        Dictionary<string, string> own = part.Groups.Where(g => items.ContainsKey(g.Name))
            .ToDictionary(g => g.Name, g => items[g.Name]);
        instances[id] = new InstanceData(id, definition.PartIds[part], part.Name, parent, path, own);
        return id;
    }

    private string Relative(string file)
    {
        string relative = Path.IsPathRooted(file) ? Path.GetRelativePath(architecture.App.AppRoot, file) : file;
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}

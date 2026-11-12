using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure;

public sealed class AppGraph
{
    public const int MaxRows = 3;

    private readonly AppImport.FileDto file;
    private readonly Dictionary<string, AppImport.InstanceDto> instances;
    private readonly Dictionary<string, List<AppImport.TypeDto>> typesOf;
    private readonly HashSet<string> shown;
    private readonly Dictionary<string, RowPlace> rowOfType = [];

    private AppGraph(AppImport.FileDto file)
    {
        this.file = file;
        instances = file.App.Instances.ToDictionary(i => i.Id);
        typesOf = file.App.Types.GroupBy(t => t.Instance)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Name, StringComparer.Ordinal).ToList());
        shown = [.. typesOf.Keys.SelectMany(Lineage)];
    }

    public static LayoutData Of(string json)
    {
        return new AppGraph(AppImport.Read(json)).Layout();
    }

    private static string Last(string path)
    {
        return path.Split('/')[^1];
    }

    private LayoutData Layout()
    {
        List<AppImport.InstanceDto> visible = [.. file.App.Instances.Where(i => shown.Contains(i.Id))];
        HashSet<string> parents = [.. visible.Select(i => i.Parent).OfType<string>()];
        List<Group> groups = [.. visible.Where(i => parents.Contains(i.Id)).Select(GroupOf)];
        List<Node> nodes = [.. visible.Where(i => !parents.Contains(i.Id)).Select(NodeOf)];
        return new LayoutData(new Size(0, 0), groups, nodes, Edges());
    }

    private IEnumerable<string> Lineage(string id)
    {
        for (string? at = id; at is not null; at = instances[at].Parent)
        {
            yield return at;
        }
    }

    private Group GroupOf(AppImport.InstanceDto instance)
    {
        int depth = Lineage(instance.Id).Count() - 1;
        string title = instance.Parent is null ? $"{file.App.Name} app"
            : IsMany(instance) ? Last(instance.Path) : instance.Layer;
        string kind = instance.Parent is null ? "App root" : IsMany(instance) ? instance.Layer : "Folder";
        return new Group(instance.Id, instance.Parent, title, kind, instance.Path, depth, 0, 0, 0, 0);
    }

    private Node NodeOf(AppImport.InstanceDto instance)
    {
        List<AppImport.TypeDto> types = typesOf[instance.Id];
        int count = types.Count > MaxRows + 1 ? MaxRows : types.Count;
        List<Row> rows = [.. types.Take(count)
            .Select(t => new Row(t.Id, t.Name, TypeRoles.Of(t.Name, instance.Layer), false, [t.Id]))];
        if (count < types.Count)
        {
            List<string> rest = [.. types.Skip(count).Select(t => t.Id)];
            rows.Add(new Row($"{instance.Id}#more", $"+ {rest.Count} types", "more", true, rest));
        }
        foreach (Row row in rows)
        {
            foreach (string type in row.Types)
            {
                rowOfType[type] = new RowPlace(instance.Id, row.Id);
            }
        }
        string group = GroupName(instance);
        return new Node(instance.Id, instance.Parent!, instance.Layer, group, instance.Path, 0, 0, 0, 0, rows);
    }

    private List<Edge> Edges()
    {
        Dictionary<string, string> names = file.App.Types.ToDictionary(t => t.Id, t => t.Name);
        List<AppImport.UseDto> uses = [.. file.App.Dependencies.SelectMany(d => d.Uses)
            .Where(u => rowOfType.ContainsKey(u.From) && rowOfType.ContainsKey(u.To))
            .Where(u => rowOfType[u.From].Node != rowOfType[u.To].Node)];
        return [.. uses.GroupBy(u => (From: rowOfType[u.From], To: rowOfType[u.To])).Select((g, i) => new Edge(
            $"e{i}",
            g.Key.From.Node,
            g.Key.From.Row,
            g.Key.To.Node,
            g.Key.To.Row,
            g.Any(u => u.Access == Access.Deny) ? Access.Deny : Access.Allow,
            [.. g.Select(u => new Use(names[u.From], names[u.To], u.Access, RuleText(u.Rule), false))],
            []))];
    }

    private string RuleText(string? rule)
    {
        return rule is null ? $"no rule allows it (Unlisted = {file.Definition.Unlisted})"
            : file.Definition.Rules.First(r => r.Id == rule).Text;
    }

    private string GroupName(AppImport.InstanceDto instance)
    {
        foreach (string id in Lineage(instance.Id).Skip(1))
        {
            AppImport.InstanceDto at = instances[id];
            if (IsMany(at))
            {
                return Last(at.Path);
            }
            if (at.Layer == "App")
            {
                return "App";
            }
        }
        return Last(instance.Path);
    }

    private bool IsMany(AppImport.InstanceDto instance)
    {
        return file.Definition.Parts.First(p => p.Id == instance.Part).Many;
    }

    private sealed record RowPlace(string Node, string Row);
}

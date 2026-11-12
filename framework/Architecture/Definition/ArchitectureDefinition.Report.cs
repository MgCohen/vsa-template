using System.Text;

namespace VSA.Architecture;

public abstract partial class ArchitectureDefinition {
    public string Report() {
        Resolve();
        var text = new StringBuilder($"Options: DefaultPermission = {Options.DefaultPermission}, Own = {Options.Own}\n\n");
        foreach (var line in summary) {
            text.AppendLine($"{line.Permission,-5}  {line.Rule,-100} {line.Count,4} pairs  [{line.Source}]");
        }
        text.AppendLine($"\nConflicts (the later rule wins): {conflicts.Distinct().Count()}");
        foreach (var conflict in conflicts.Distinct().Take(4)) {
            text.AppendLine("  " + conflict);
        }
        return text.ToString();
    }

    public string Explain(string path, string layer) {
        var text = new StringBuilder($"{layer} in {path}:");
        var rows = Resolve().Where(p => p.From.Path == path && p.From.Name == layer)
            .GroupBy(p => (p.To.Name, p.To.Path)).OrderBy(g => g.Key.Path).ThenBy(g => g.Key.Name);
        foreach (var row in rows) {
            var steps = string.Join("  then  ", row.Select(p => $"{p.Permission} if {p.Condition} [{p.Source}]"));
            text.Append($"\n  {row.Key.Name,-20} {row.Key.Path,-36} {steps}");
        }
        text.Append($"\n  any other part: {Options.DefaultPermission}");
        return text.ToString();
    }

    public string Tree() {
        var text = new StringBuilder(Root.Path.Length == 0 ? "." : Root.Path);
        Draw(Root, "", text);
        return text.ToString();
    }

    static void Draw(Part node, string indent, StringBuilder text) {
        for (var i = 0; i < node.Children.Count; i++) {
            var child = node.Children[i];
            var last = i == node.Children.Count - 1;
            var mark = (child.IsGroup ? " [group]" : child.HasFolder ? "" : " [parent folder]")
                + (child.Types.Count == 0 ? "" : " : " + string.Join(" | ", child.Types.Select(t => t.Name)));
            text.Append('\n').Append(indent).Append(last ? "└─ " : "├─ ").Append(child.Name).Append(mark);
            Draw(child, indent + (last ? "   " : "│  "), text);
        }
    }
}

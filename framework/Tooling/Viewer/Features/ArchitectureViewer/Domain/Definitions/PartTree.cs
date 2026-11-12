using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Definitions;

public sealed class PartTree(IReadOnlyList<Part> parts)
{
    private readonly Dictionary<string, Part> byId = parts.ToDictionary(p => p.Id);

    public Part this[string id] => byId[id];

    public IEnumerable<Part> ByLayer(string layer)
    {
        return parts.Where(p => p.Layer == layer && p.Parent is not null);
    }

    public bool HasChildren(string id)
    {
        return parts.Any(p => p.Parent == id);
    }

    public bool Within(string id, string ancestor)
    {
        for (string? at = id; at is not null; at = byId[at].Parent)
        {
            if (at == ancestor)
            {
                return true;
            }
        }
        return false;
    }

    public int Depth(string id)
    {
        return byId[id].Parent is { } parent ? Depth(parent) + 1 : 0;
    }

    public string Common(string a, string b)
    {
        string at = a;
        while (!Within(b, at))
        {
            at = byId[at].Parent!;
        }
        return at;
    }
}

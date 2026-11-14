namespace ArchitectureViewer.Presenters.Items;

public sealed record LinkItem(
    string Id,
    string FromNode,
    string ToNode,
    string Access,
    string? Label,
    bool Pending,
    bool Removed,
    IReadOnlyList<double[]> Route,
    IReadOnlyList<UseItem> Uses)
{
    public bool Denied => Access == "Deny";

    public bool Unchecked => Access == "Unchecked";

    public bool Conditional => Access == "Conditional";

    public bool Merged => Uses.Count(u => u.EdgeId is not null) > 1;
}

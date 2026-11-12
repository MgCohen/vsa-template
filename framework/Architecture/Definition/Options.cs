namespace VSA.Architecture;

public sealed class Options {
    public Access Unlisted { get; set; } = Access.Deny;
    public Own Own { get; set; } = Own.AllGroups;
    public Reach Reach { get; set; } = Reach.WithChildren;
}

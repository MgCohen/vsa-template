namespace VSA.Architecture;

public sealed class Options {
    public Permission Unlisted { get; set; } = Permission.Deny;
    public Own Own { get; set; } = Own.AllGroups;
    public Reach Reach { get; set; } = Reach.WithChildren;
    public Access Access { get; set; } = Access.Public;
}

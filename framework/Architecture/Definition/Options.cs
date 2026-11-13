namespace VSA.Architecture;

public sealed class Options {
    public Permission DefaultPermission { get; set; } = Permission.Deny;
    public Boundary Boundary { get; set; } = Boundary.Open;
    public bool UseSameLayer { get; set; }
    public Depth Depth { get; set; } = Depth.WithChildren;
    public Access Access { get; set; } = Access.Public;
}

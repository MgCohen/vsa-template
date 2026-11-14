namespace VSA.Architecture;

public sealed class Options {
    public Permission DefaultPermission { get; set; } = Permission.Deny;
    public Boundary Boundary { get; set; } = Boundary.Open;
    public bool UseSameConcept { get; set; }
    public Depth Depth { get; set; } = Depth.WithChildren;
    public Access Access { get; set; } = Access.Public;
}

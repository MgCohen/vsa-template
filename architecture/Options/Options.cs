namespace Umpire.Architecture;

public sealed class Options {
    public static Options BuiltIn => new() {
        DefaultPermission = Permission.Deny,
        Boundary = Umpire.Architecture.Boundary.Open,
        UseSameConcept = false,
        Depth = Umpire.Architecture.Depth.WithChildren,
        Access = Umpire.Architecture.Access.Public,
    };

    public Permission? DefaultPermission { get; set; }
    public Boundary? Boundary { get; set; }
    public bool? UseSameConcept { get; set; }
    public Depth? Depth { get; set; }
    public Access? Access { get; set; }

    internal Options Over(Options fallback) => new() {
        DefaultPermission = DefaultPermission ?? fallback.DefaultPermission,
        Boundary = Boundary ?? fallback.Boundary,
        UseSameConcept = UseSameConcept ?? fallback.UseSameConcept,
        Depth = Depth ?? fallback.Depth,
        Access = Access ?? fallback.Access,
    };
}

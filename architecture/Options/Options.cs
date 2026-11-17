namespace Umpire.Architecture;

public sealed class Options {
    public static Options BuiltIn => new() {
        DefaultPermission = Permission.Deny,
        Boundary = Umpire.Architecture.Boundary.Open,
        UseSameConcept = false,
        Depth = Umpire.Architecture.Depth.WithChildren,
        Access = Umpire.Architecture.Access.Public,
        Required = false,
    };

    public Permission? DefaultPermission { get; set; }
    public Boundary? Boundary { get; set; }
    public bool? UseSameConcept { get; set; }
    public Depth? Depth { get; set; }
    public Access? Access { get; set; }
    public bool? Required { get; set; }

    internal static Options From(Action<Options> configure) {
        var options = new Options();
        configure(options);
        return options;
    }

    internal Options Over(Options fallback) => new() {
        DefaultPermission = DefaultPermission ?? fallback.DefaultPermission,
        Boundary = Boundary ?? fallback.Boundary,
        UseSameConcept = UseSameConcept ?? fallback.UseSameConcept,
        Depth = Depth ?? fallback.Depth,
        Access = Access ?? fallback.Access,
        Required = Required ?? fallback.Required,
    };
}

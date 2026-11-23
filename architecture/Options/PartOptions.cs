namespace Umpire.Architecture;

public sealed class PartOptions {
    public static PartOptions BuiltIn => new() {
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

    internal static PartOptions From(Action<PartOptions> configure) {
        var options = new PartOptions();
        configure(options);
        return options;
    }

    internal PartOptions Over(PartOptions fallback) => new() {
        DefaultPermission = DefaultPermission ?? fallback.DefaultPermission,
        Boundary = Boundary ?? fallback.Boundary,
        UseSameConcept = UseSameConcept ?? fallback.UseSameConcept,
        Depth = Depth ?? fallback.Depth,
        Access = Access ?? fallback.Access,
        Required = Required ?? fallback.Required,
    };
}

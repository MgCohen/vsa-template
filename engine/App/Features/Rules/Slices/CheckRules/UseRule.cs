namespace Rules.Slices.CheckRules;

using Umpire.Architecture;

abstract class UseRule : ArchitectureRule {
    public sealed override int Group => 1;

    protected static Binding? Other(ArchitectureCheck check, Binding from, string target) =>
        check.Bindings.Of(target) is { } to && !(to.Part == from.Part && to.Path == from.Path) ? to : null;
}

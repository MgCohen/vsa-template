using ArchUnitNET.Fluent.Syntax.Elements.Types;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace VSA.Testing.Architecture;

public static class LayerExtensions {
    public static GivenTypesConjunction AreIn<TLayer>(this GivenTypesThat that, ArchitectureMap map) where TLayer : Layer =>
        that.Are(map.In<TLayer>());

    public static GivenTypesConjunction AreIn(this GivenTypesThat that, PartInstance instance) =>
        that.Are(instance.Types);

    public static TypesShouldConjunction NotDependOnAny<TLayer>(this TypesShould should, ArchitectureMap map) where TLayer : Layer =>
        should.NotDependOnAny(map.In<TLayer>());

    public static TypesShouldConjunction NotDependOnAnyDeniedTo(this TypesShould should, PartInstance instance) =>
        should.NotDependOnAny(instance.Denied);

    public static TypesShouldConjunction ExposeOnlyWithinLimitsOf(this TypesShould should, PartInstance instance) =>
        should.FollowCustomCondition(
            type => Exposures.Of(type, instance.Framework.Contains).All(e => !instance.Limits.TryGetValue(e.Target, out var limit) || e.Access.IsWithin(limit)),
            "expose other parts only within the access of their rules",
            "exposes a type of another part with more access than its rule allows");
}

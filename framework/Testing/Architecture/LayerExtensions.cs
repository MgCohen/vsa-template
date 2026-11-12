using ArchUnitNET.Fluent.Syntax.Elements.Types;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace VSA.Testing.Architecture;

public static class LayerExtensions {
    public static GivenTypesConjunction AreIn<TLayer>(this GivenTypesThat that) where TLayer : Layer =>
        that.Are(ArchitectureScope.Current.In<TLayer>());

    public static GivenTypesConjunction AreIn(this GivenTypesThat that, PartInstance instance) =>
        that.Are(instance.Types);

    public static TypesShouldConjunction NotDependOnAny<TLayer>(this TypesShould should) where TLayer : Layer =>
        should.NotDependOnAny(ArchitectureScope.Current.In<TLayer>());

    public static TypesShouldConjunction NotDependOnAnyDeniedTo(this TypesShould should, PartInstance instance) =>
        should.NotDependOnAny(instance.Denied);
}

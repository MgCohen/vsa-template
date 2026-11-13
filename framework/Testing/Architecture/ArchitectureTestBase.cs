using ArchUnitNET.Domain;
using NUnit.Framework;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace VSA.Testing.Architecture;

public abstract class ArchitectureTestBase : ArchitectureTestBase<ArchitectureDefinition>;

public abstract class ArchitectureTestBase<TDefinition> where TDefinition : ArchitectureDefinition {
    static readonly Lazy<ArchitectureMap> LoadedMap = new(() => Load(typeof(TDefinition)));

    protected static ArchitectureMap Map => LoadedMap.Value;

    protected static IEnumerable<IType> AppTypes => Map.AppTypes;

    protected static IEnumerable<IType> BoundTypes => Map.BoundTypes;

    static ArchitectureMap Load(Type definition) {
        var map = ArchitectureMap.Load(definition);
        foreach (var warning in map.Warnings) {
            TestContext.Progress.WriteLine(warning);
        }
        return map;
    }
}

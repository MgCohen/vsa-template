namespace EngineCore.Tests;

using System.Reflection;
using Rules.Slices.CheckRules;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class ArchitectureTestEngineTests : ArchitectureTest<VsaArchitecture> {
    static readonly string Block = FixtureApps.Block("VsaApp");

    static readonly Lazy<ArchitectureCheck> Check =
        new(() => FixtureApps.Load(new VsaArchitecture(), AppContext.BaseDirectory, Block));

    static readonly Assembly App = Assembly.Load("VsaApp");

    [Test]
    public void The_base_binds_each_type_of_the_VSA_fixture_to_the_part_of_the_engine() {
        var bindings = Check.Value.Bindings.All.Where(b => !b.External && b.Type.Assembly == "VsaApp").ToList();

        var differences = bindings
            .Select(b => new Difference(b.Type.FullName, b.Part.Name, BasePart(b)))
            .Where(d => d.Engine != d.Base);

        Assert.That(bindings.Where(b => b.FollowsUser), Is.Not.Empty);
        Assert.That(differences, Is.Empty);
    }

    [Test]
    public void The_base_does_not_map_each_type_that_the_engine_does_not_map() {
        var unmapped = Check.Value.Bindings.Unmapped;

        var mapped = unmapped
            .Where(u => BasePlace(u.Type.FullName, FolderOf(u.Source)).IsMapped)
            .Select(u => u.Type.FullName);

        Assert.That(unmapped, Is.Not.Empty);
        Assert.That(mapped, Is.Empty);
    }

    static string FolderOf(Source source) =>
        Path.GetRelativePath(Block, Path.GetDirectoryName(source.File)!).Replace('\\', '/');

    static IEnumerable<string> TypesIn(string folder) =>
        Check.Value.Bindings.AppTypes.Where(t => t.Source is { } s && FolderOf(s) == folder).Select(t => t.FullName);

    string BasePart(Binding binding) => BasePlace(binding.Type.FullName, FolderOf(binding.Source!)).Part;

    Place BasePlace(string fullName, string folder) {
        var neighbors = TypesIn(folder)
            .Where(name => name != fullName)
            .Select(name => App.GetType(name, true)!)
            .ToArray();
        return At(App.GetType(fullName, true)!, folder, neighbors);
    }

    sealed record Difference(string Type, string Engine, string Base);
}

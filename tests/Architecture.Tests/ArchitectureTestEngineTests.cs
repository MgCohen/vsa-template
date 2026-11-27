namespace EngineCore.Tests;

extern alias testing;

using System.Reflection;
using Umpire.CheckModel;
using Umpire.Vsa.Definition;
using testing::Umpire.Architecture;

public sealed class ArchitectureTestEngineTests : ArchitectureTest<VsaArchitecture> {
    static readonly string Block = FixtureApps.Block("VsaApp");

    static readonly Lazy<CheckedApp> Check =
        new(() => FixtureApps.Load(new VsaArchitecture(), AppContext.BaseDirectory, Block));

    static readonly Assembly App = Assembly.Load("VsaApp");

    [Test]
    public void The_base_binds_each_type_of_the_VSA_fixture_to_the_part_of_the_engine() {
        var placed = Check.Value.Types.Where(t => t.Project == "VsaApp" && !t.IsUnmapped)
            .Select(t => new Placed(t, Check.Value.Model.PlacementOf(t)!)).ToList();

        var differences = placed
            .Select(p => new Difference(p.Type.Name.FullName, p.Placement.Part.Name, BasePart(p.Type)))
            .Where(d => d.Engine != d.Base);

        Assert.That(placed.Where(p => p.Placement.FollowsUser), Is.Not.Empty);
        Assert.That(differences, Is.Empty);
    }

    [Test]
    public void The_base_does_not_map_each_type_that_the_engine_does_not_map() {
        var unmapped = Check.Value.Unmapped;

        var mapped = unmapped
            .Where(u => BasePlace(u.Name.FullName, FolderOf(u.File)).IsMapped)
            .Select(u => u.Name.FullName);

        Assert.That(unmapped, Is.Not.Empty);
        Assert.That(mapped, Is.Empty);
    }

    static string FolderOf(string file) =>
        Path.GetRelativePath(Block, Path.GetDirectoryName(file)!).Replace('\\', '/');

    static IEnumerable<string> TypesIn(string folder) =>
        Check.Value.Types.Where(t => FolderOf(t.File) == folder).Select(t => t.Name.FullName);

    string BasePart(AppType type) => BasePlace(type.Name.FullName, FolderOf(type.File)).Part;

    Place BasePlace(string fullName, string folder) {
        var neighbors = TypesIn(folder)
            .Where(name => name != fullName)
            .Select(name => App.GetType(name, true)!)
            .ToArray();
        return At(App.GetType(fullName, true)!, folder, neighbors);
    }

    sealed record Placed(AppType Type, Placement Placement);

    sealed record Difference(string Type, string Engine, string Base);
}

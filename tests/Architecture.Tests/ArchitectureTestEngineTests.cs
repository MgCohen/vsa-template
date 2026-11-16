namespace EngineCore.Tests;

using System.Reflection;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class ArchitectureTestEngineTests : ArchitectureTest<VsaArchitecture> {
    static readonly string Block = FixtureApps.Block("VsaApp");

    [Test]
    public void The_base_binds_each_type_of_the_VSA_fixture_to_the_part_of_the_engine() {
        var app = Assembly.Load("VsaApp");
        var bindings = EngineBindings().ToList();

        var differences = bindings
            .Select(b => new Difference(b.Type.FullName, b.Part.Name, BasePart(app, b)))
            .Where(d => d.Engine != d.Base);

        Assert.That(bindings, Is.Not.Empty);
        Assert.That(differences, Is.Empty);
    }

    static IEnumerable<Binding> EngineBindings() =>
        FixtureApps.Load(new VsaArchitecture(), AppContext.BaseDirectory, Block).Bindings.All
            .Where(b => !b.External && !b.FollowsUser && b.Type.Assembly == "VsaApp");

    static string FolderOf(Binding binding) =>
        Path.GetRelativePath(Block, Path.GetDirectoryName(binding.Source!.File)!).Replace('\\', '/');

    string BasePart(Assembly app, Binding binding) =>
        At(app.GetType(binding.Type.FullName, true)!, FolderOf(binding)).Part;

    sealed record Difference(string Type, string Engine, string Base);
}

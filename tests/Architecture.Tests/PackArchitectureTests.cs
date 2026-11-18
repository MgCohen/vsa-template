namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Kernels;

public sealed class PackArchitectureTests {
    const string PackFolder = "C:/packs/Sample";

    [TestCase("Architecture/SampleArchitecture.cs")]
    [TestCase("Kernel/Hosting/SampleHost.cs")]
    [TestCase("Tests/Unit/SampleTests.cs")]
    public void The_three_pack_folders_are_parts(string file) {
        var bindings = Place(Pack(), Fact(file, "Other"));

        Assert.That(bindings.Unmapped, Is.Empty);
    }

    [Test]
    public void A_file_outside_the_three_folders_is_unmapped() {
        var bindings = Place(Pack(), Fact("Other/Stray.cs", "Stray"));

        Assert.That(bindings.Unmapped.Select(u => u.Type.Name), Is.EqualTo(new[] { "Stray" }));
    }

    [Test]
    public void The_pack_class_is_in_the_pack_folder() {
        var bindings = Place(Pack());

        Assert.That(bindings.All.Single().Part.Is(typeof(Umpire.Architecture.Blocks.Module)), Is.True);
    }

    [Test]
    public void A_pack_with_no_pack_class_fails_the_count() {
        var resolved = new ResolvedArchitecture(new PackArchitecture());

        var counts = resolved.Counts(resolved.Place([Fact("Kernel/SampleHost.cs", "SampleHost")], PackFolder));

        Assert.That(counts, Has.Count.EqualTo(1));
    }

    [Test]
    public void The_pack_base_is_the_check_entry_of_the_pack_architecture() {
        Assert.That(typeof(Pack).BaseType, Is.EqualTo(typeof(Block<PackArchitecture>)));
        Assert.That(typeof(Pack<,,>).IsAbstract, Is.True);
    }

    static Bindings Place(params TypeFacts[] types) => new ResolvedArchitecture(new PackArchitecture()).Place(types, PackFolder);

    static TypeFacts Pack() =>
        new("Sample.SamplePack", "SamplePack", "Sample", new Source($"{PackFolder}/SamplePack.cs", 1), [typeof(Pack).FullName!], [], [], []);

    static TypeFacts Fact(string file, string name) =>
        new($"Sample.{name}", name, "Sample", new Source($"{PackFolder}/{file}", 1), [], [], [], []);
}

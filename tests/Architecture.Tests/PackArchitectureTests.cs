namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Kernels;
using Umpire.Vsa.Definition;

public sealed class PackArchitectureTests {
    const string PackFolder = "C:/packs/Sample";

    [TestCase("Definition/SampleArchitecture.cs")]
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

        Assert.That(bindings.All.Single().Part.Concept, Is.TypeOf<Umpire.Architecture.Blocks.App>());
    }

    [Test]
    public void A_pack_with_no_pack_class_fails_the_count() {
        var resolved = new ResolvedArchitecture(new PackArchitecture());

        var counts = resolved.Counts(resolved.Place([Fact("Kernel/SampleHost.cs", "SampleHost")], PackFolder));

        Assert.That(counts, Has.Count.EqualTo(1));
    }

    [Test]
    public void A_pack_with_code_only_in_its_kernel_fails_the_count() {
        var resolved = new ResolvedArchitecture(new PackArchitecture(new Umpire.Vsa.Definition.VsaArchitecture()));

        var counts = resolved.Counts(resolved.Place([Fact("Kernel/Domain/Entity.cs", "Entity")], PackFolder));

        Assert.That(counts.Select(c => c.Message), Has.Some.Contains("types of Pack"));
    }

    [Test]
    public void The_pack_base_is_the_check_entry_of_the_pack_architecture() {
        Assert.That(typeof(Pack).BaseType, Is.EqualTo(typeof(Block<PackArchitecture>)));
        Assert.That(typeof(Pack<,,>).IsAbstract, Is.True);
    }

    [Test]
    public void A_pack_with_its_architecture_has_the_kernel_of_that_architecture_as_folder_parts() {
        var kernel = WithKernel().Parts.Where(p => p.Path.StartsWith("Kernel/", StringComparison.Ordinal)).ToList();

        Assert.That(kernel.Select(p => p.Path), Does.Contain("Kernel/Domain").And.Contain("Kernel/Handlers/Commands"));
        Assert.That(kernel, Has.All.Matches<Part>(p => !p.IsExternal && p.Types.Count == 0));
    }

    [Test]
    public void A_type_in_a_kernel_layer_folder_binds_to_that_layer() {
        var bindings = WithKernel().Place([Fact("Kernel/Handlers/Commands/CommandHandler.cs", "CommandHandler")], PackFolder);

        Assert.That(bindings.All.Single().Part.Name, Is.EqualTo("Handlers.Commands"));
    }

    [Test]
    public void Each_use_between_two_kernel_layers_is_allowed_until_the_kernel_has_rules() {
        var resolved = WithKernel();
        var layers = resolved.Parts.Where(p => p.Path.StartsWith("Kernel/", StringComparison.Ordinal)).ToList();
        var none = new Dictionary<string, string>();

        var denied = layers.SelectMany(from => layers.Where(to => to != from)
                .Where(to => resolved.DecisionFor(from, none, to, none).Permission != Permission.Allow)
                .Select(to => $"{from.Path} -> {to.Path}"))
            .ToList();

        Assert.That(layers, Has.Count.EqualTo(14));
        Assert.That(denied, Is.Empty);
    }

    static ResolvedArchitecture WithKernel() => new(new PackArchitecture(new VsaArchitecture()));

    static Bindings Place(params TypeFacts[] types) => new ResolvedArchitecture(new PackArchitecture()).Place(types, PackFolder);

    static TypeFacts Pack() =>
        new("Sample.SamplePack", "SamplePack", "Sample", new Source($"{PackFolder}/SamplePack.cs", 1), [typeof(Pack).FullName!], [], [], []);

    static TypeFacts Fact(string file, string name) =>
        new($"Sample.{name}", name, "Sample", new Source($"{PackFolder}/{file}", 1), [], [], [], []);
}

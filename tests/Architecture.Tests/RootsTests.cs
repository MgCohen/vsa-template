namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;
using Umpire.Vsa.Definition;

public sealed class RootsTests {
    [Test]
    public void Each_architecture_has_the_app_root_and_the_definition_root() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(root => root.Add<Child>()));

        Assert.That(resolved.Root.Children.Select(p => p.Name), Is.EqualTo(new[] { "App", "Definition" }));
    }

    [Test]
    public void Define_writes_into_the_app_root() {
        var resolved = new ResolvedArchitecture(new InlineArchitecture(root => root.Add<Child>()));

        Assert.That(resolved.Parts.Single(p => p.Concept is Child).Parent!.Concept, Is.TypeOf<App>());
    }

    [Test]
    public void An_empty_kernel_root_is_not_a_part() {
        var parts = new ResolvedArchitecture(new InlineArchitecture(root => root.Add<Child>())).Parts;

        Assert.That(parts.Any(p => p.Concept is Kernel), Is.False);
    }

    [Test]
    public void The_VSA_definition_adds_the_core_root_the_view_root_and_its_kernel() {
        var roots = new ResolvedArchitecture(new VsaArchitecture()).Root.Children.Where(p => !p.IsExternal).Select(p => p.Name);

        Assert.That(roots, Is.EqualTo(new[] { "App", "Definition", "Core", "Views", "Kernel" }));
    }

    [Test]
    public void A_root_that_two_levels_add_is_one_root() {
        var roots = new ResolvedArchitecture(new TwoLevelViews()).Root.Children.Count(p => p.Concept is Views);

        Assert.That(roots, Is.EqualTo(1));
    }

    sealed class TwoLevelViews : VsaArchitecture {
        protected override void Configure(ArchitectureConfig config) => config.Roots.Add<Views>();
    }
}

using BuiltApp.Infrastructure;
using Tests.Shared;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

namespace Engine.Tests;

public sealed class EmbeddedPdbTests {
    static readonly string Bundle = Shipped("Umpire");
    static readonly string VsaBundle = Shipped("Umpire.Vsa.Kernel", "Umpire.Vsa");
    static readonly string DefinitionBundle = Shipped("Umpire.Definition");

    [Test]
    public void The_shipped_assembly_gives_the_source_path_of_each_type_under_the_mapped_root() {
        var sources = BinReader.Sources([VsaBundle]);

        Assert.That(sources["Umpire.Vsa.Contracts.Requests.IRequestSender"].File,
            Does.StartWith("/_/packs/Vsa/Kernel/Contracts/Requests/"));
    }

    [Test]
    public void A_record_with_no_body_in_the_shipped_definition_assembly_has_its_source_path() {
        var sources = BinReader.Sources([DefinitionBundle]);

        Assert.That(sources["Umpire.Architecture.Blocks.Definition"].File, Does.StartWith("/_/architecture/Concepts/Blocks/"));
    }

    [Test]
    public void The_shipped_runtime_assembly_holds_no_definition_type() {
        var sources = BinReader.Sources([Bundle]);

        Assert.That(sources.Values.Select(source => source.File), Has.None.Matches<string>(IsDefinitionSource));
    }

    [Test]
    public void The_shipped_core_assembly_holds_no_VSA_type() {
        var sources = BinReader.Sources([Bundle]);

        Assert.That(sources.Keys, Has.None.StartsWith("Umpire.Vsa."));
        Assert.That(sources.Values.Select(source => source.File), Has.None.StartsWith("/_/packs/"));
    }

    [Test]
    public void The_shipped_VSA_assembly_holds_only_the_VSA_kernel() {
        var files = BinReader.Sources([VsaBundle]).Values.Select(source => source.File)
            .Where(file => !file.StartsWith("/_/tools/sdk/Polyfills~/", StringComparison.Ordinal)).ToList();

        Assert.That(files, Is.Not.Empty.And.All.StartsWith("/_/packs/Vsa/Kernel/"));
        Assert.That(files, Has.None.StartsWith("/_/packs/Vsa/Kernel/Views/"));
    }

    [Test]
    public void A_type_of_the_shipped_assembly_binds_to_the_body_of_its_kernel_block() {
        var source = BinReader.Sources([VsaBundle])["Umpire.Vsa.Contracts.Requests.IRequestSender"];
        var type = new TypeFacts("Umpire.Vsa.Contracts.Requests.IRequestSender", "IRequestSender", "Umpire.Vsa", source, [], [], [], []);

        var bound = new ResolvedArchitecture(new VsaArchitecture()).Place([type], Path.GetFullPath("Demo")).Of(type.FullName);

        Assert.That(bound?.Part.SelfAndAncestors().Select(p => p.Name), Is.EqualTo(new[] { "Contracts.Requests", "Contracts", "Kernel", "(root)" }));
    }

    static bool IsDefinitionSource(string file) =>
        (file.StartsWith("/_/architecture/", StringComparison.Ordinal) && !file.StartsWith("/_/architecture/Markers/", StringComparison.Ordinal))
        || file.StartsWith("/_/framework/Kernel/Definition/", StringComparison.Ordinal)
        || file.StartsWith("/_/packs/Vsa/Definition/", StringComparison.Ordinal);

    static string Shipped(string assembly) => Shipped(assembly, assembly);

    static string Shipped(string project, string assembly) => Directory
        .GetFiles(Repo.Path("artifacts~", "bin", project), $"{assembly}.dll", SearchOption.AllDirectories)
        .OrderByDescending(File.GetLastWriteTimeUtc).First();
}

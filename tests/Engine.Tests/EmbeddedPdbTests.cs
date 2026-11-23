using BuiltApp.Infrastructure;
using Tests.Shared;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

namespace Engine.Tests;

public sealed class EmbeddedPdbTests {
    static readonly string Bundle = Directory.GetFiles(Repo.Path("artifacts~", "bin", "Umpire"), "Umpire.dll", SearchOption.AllDirectories)
        .OrderByDescending(File.GetLastWriteTimeUtc).First();

    [Test]
    public void The_shipped_assembly_gives_the_source_path_of_each_type_under_the_mapped_root() {
        var sources = BinReader.Sources([Bundle]);

        Assert.That(sources["Umpire.Vsa.Contracts.Requests.IRequestSender"].File,
            Does.StartWith("/_/packs/Vsa/Kernel/Contracts/Requests/"));
    }

    [Test]
    public void A_type_of_the_shipped_assembly_binds_to_the_body_of_its_kernel_block() {
        var source = BinReader.Sources([Bundle])["Umpire.Vsa.Contracts.Requests.IRequestSender"];
        var type = new TypeFacts("Umpire.Vsa.Contracts.Requests.IRequestSender", "IRequestSender", "Umpire", source, [], [], [], []);

        var bound = new ResolvedArchitecture(new VsaArchitecture()).Place([type], Path.GetFullPath("Demo")).Of(type.FullName);

        Assert.That(bound?.Part.SelfAndAncestors().Select(p => p.Name), Is.EqualTo(new[] { "Contracts.Requests", "Contracts", "VsaKernel", "(root)" }));
    }
}

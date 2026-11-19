using Blocks.Slices.FindBlocks;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Tests.Shared;

namespace Engine.Tests;

public sealed class UsedBlocksTests {
    static readonly string Pack = Repo.Path("packs", "Vsa");
    static BinIndex? index;

    static BinIndex Index {
        get {
            if (index is null) {
                using var reader = new BinReader(AppContext.BaseDirectory);
                index = reader.Index();
            }
            return index;
        }
    }

    [TestCase("tests", "Fixtures", "Notes")]
    [TestCase("engine")]
    public void A_block_uses_the_pack_block_of_its_project_references_and_no_other_block_of_the_bin(params string[] folder) {
        var used = UsedBlocks.Of(Repo.Path(folder), [], Index.Entries, Index);

        Assert.That(used, Is.EqualTo(new[] { Path.GetFullPath(Pack) }));
    }

    [Test]
    public void An_inner_block_is_a_used_block() {
        var app = Repo.Path("tests", "Fixtures", "PackVariant");
        var inner = Index.Entries.Where(e => e.Folder.StartsWith(Path.Combine(app, "Packs"), StringComparison.OrdinalIgnoreCase)).ToList();

        var used = UsedBlocks.Of(app, inner, Index.Entries, Index);

        Assert.That(used, Does.Contain(Path.Combine(app, "Packs", "Ecs")));
    }

    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public void A_block_in_a_package_is_not_a_used_block(bool isProject, int count) {
        var root = TempFolder.Create();
        try {
            var app = Directory.CreateDirectory(Path.Combine(root, "App")).FullName;
            var lib = Directory.CreateDirectory(Path.Combine(root, "Lib")).FullName;
            var appDll = Dll("App", Path.Combine(app, "App.cs"), ["Lib"], isProject: true);
            var libDll = Dll("Lib", Path.Combine(lib, "Lib.cs"), [], isProject);
            var entry = new BlockEntry(lib, EntryForm.Metadata, null, Path.Combine(lib, "Lib.csproj"));
            var bin = new BinIndex(root, [appDll, libDll], [entry]);

            var used = UsedBlocks.Of(app, [], bin.Entries, bin);

            Assert.That(used, Has.Count.EqualTo(count));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void The_deps_file_gives_the_assemblies_of_the_project_references_only() {
        var bin = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(bin, "App.deps.json"), """
                {
                  "targets": { ".NETCoreApp,Version=v10.0": {
                    "App/1.0.0": { "runtime": { "App.dll": {} } },
                    "Lib/1.0.0": { "runtime": { "Lib.Core.dll": {} } },
                    "Pkg/2.0.0": { "runtime": { "lib/net10.0/Pkg.dll": {} } }
                  } },
                  "libraries": {
                    "App/1.0.0": { "type": "project" },
                    "Lib/1.0.0": { "type": "project" },
                    "Pkg/2.0.0": { "type": "package" }
                  }
                }
                """);

            Assert.That(DepsFile.ProjectAssemblies(bin), Is.EquivalentTo(new[] { "App", "Lib.Core" }));
        } finally {
            TempFolder.Delete(bin);
        }
    }

    static BinDll Dll(string name, string source, IReadOnlyList<string> references, bool isProject) =>
        new(Path.Combine(Path.GetDirectoryName(source)!, name + ".dll"), name, false, references, [source], [], [], isProject);
}

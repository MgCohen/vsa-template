using Blocks.Slices.FindBlocks;
using Facts.Domain.Shared;
using Facts.Infrastructure;
using Tests.Shared;

namespace Engine.Tests;

public sealed class UsedBlocksTests {
    static readonly string[] frameworkBlocks = [
        Repo.Path("architecture"), Repo.Path("framework", "Components", "Config"),
        Repo.Path("framework", "Components", "Messages"), Repo.Path("framework", "Kernel"), Repo.Path("packs", "Vsa"),
    ];
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
    public void A_block_uses_only_the_framework_blocks_of_its_project_references(params string[] folder) {
        var used = UsedBlocks.Of(Repo.Path(folder), [], Index.Entries, Index);

        Assert.That(used, Is.EqualTo(frameworkBlocks.Select(Path.GetFullPath)));
    }

    [Test]
    public void An_inner_block_is_a_used_block() {
        var app = Repo.Path("tests", "Fixtures", "PackVariant");
        var packs = Path.Combine(app, "App", "Packs");
        var inner = Index.Entries.Where(e => e.Folder.StartsWith(packs, StringComparison.OrdinalIgnoreCase)).ToList();

        var used = UsedBlocks.Of(app, inner, Index.Entries, Index);

        Assert.That(used, Does.Contain(Path.Combine(app, "App", "Packs", "Ecs")));
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

    [Test]
    public void A_library_with_a_bad_shape_is_not_a_project_and_the_other_libraries_stay() {
        var bin = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(bin, "App.deps.json"), """
                {
                  "targets": { ".NETCoreApp,Version=v10.0": {
                    "App/1.0.0": { "runtime": { "App.dll": {} } },
                    "Number/1.0.0": { "runtime": { "Number.dll": {} } },
                    "Text/1.0.0": "not an object",
                    "Array/1.0.0": { "runtime": [ "Array.dll" ] }
                  } },
                  "libraries": {
                    "App/1.0.0": { "type": "project" },
                    "Number/1.0.0": { "type": 7 },
                    "Text/1.0.0": { "type": "project" },
                    "Array/1.0.0": { "type": "project" }
                  }
                }
                """);
            File.WriteAllText(Path.Combine(bin, "Other.deps.json"), """[ "an array, not an object" ]""");

            Assert.That(DepsFile.ProjectAssemblies(bin), Is.EquivalentTo(new[] { "App" }));
        } finally {
            TempFolder.Delete(bin);
        }
    }

    [Test]
    public void A_deps_file_that_cannot_be_read_gives_no_project() {
        if (!OperatingSystem.IsWindows()) {
            Assert.Ignore("Only Windows locks a file with FileShare.None.");
        }
        var bin = TempFolder.Create();
        try {
            var file = Path.Combine(bin, "App.deps.json");
            File.WriteAllText(file, """
                {
                  "targets": { "t": { "App/1.0.0": { "runtime": { "App.dll": {} } } } },
                  "libraries": { "App/1.0.0": { "type": "project" } }
                }
                """);
            using var locked = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.That(DepsFile.ProjectAssemblies(bin), Is.Empty);
        } finally {
            TempFolder.Delete(bin);
        }
    }

    static BinDll Dll(string name, string source, IReadOnlyList<string> references, bool isProject) {
        var dll = Path.Combine(Path.GetDirectoryName(source)!, name + ".dll");
        return new(dll, name, false, references, [source], [], [], isProject);
    }
}

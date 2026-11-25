using Blocks.Domain.Shared;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using LoadApp.Slices.LoadApp;
using Microsoft.Extensions.DependencyInjection;
using Steps.Infrastructure;
using Tests.Shared;
using Umpire.Architecture;
using Umpire.Messages.Contracts;

namespace Engine.Tests;

public sealed class LoadAppTests {
    const string VsaArchitecture = "Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa";

    [Test]
    public void The_definition_of_a_pack_entry_has_the_kernel_of_the_pack_architecture() {
        using var reader = new BinReader(AppContext.BaseDirectory);
        var packs = Repo.Path("packs", "Vsa");
        var entry = reader.Index().Entries.First(e => Same(e.Folder, packs) && e.Packed is not null);

        var definition = LoadAppHandler.Definition(new BlockTree(entry, []), [], [], new AppLoader(AppContext.BaseDirectory));

        Assert.That(new ResolvedArchitecture(definition.Value).Parts.Select(p => p.Path),
            Does.Contain("Kernel/Domain").And.Contain("Kernel/Handlers/Commands"));
    }

    [Test]
    public void A_pack_entry_with_a_block_architecture_that_cannot_take_it_gives_UMP030505() {
        var vsa = TypeName.Parse(VsaArchitecture);
        var entry = new BlockEntry(Path.GetFullPath("C:/a"), EntryForm.Definition, vsa, "C:/a/Definition/Pack.cs", Packed: vsa);

        var definition = LoadAppHandler.Definition(new BlockTree(entry, []), [], [], new AppLoader(AppContext.BaseDirectory));

        Assert.That(definition.Error!.Code, Is.EqualTo("UMP030505"));
    }

    [Test]
    public void The_definition_in_the_Definition_folder_of_the_block_wins_over_the_definition_of_the_kernel() {
        Assert.That(Find([Vsa, Own()]).Value.FullName, Is.EqualTo("A.MyArchitecture"));
    }

    [Test]
    public void An_app_with_no_definition_gets_the_definition_of_its_kernel() {
        var variant = new DefinitionType(new("B.Other", "B"), Path.GetFullPath("C:/b"), VsaKernel, [Vsa.Type.FullName]);

        Assert.That(Find([variant, Vsa]).Value, Is.EqualTo(Vsa.Type));
    }

    [Test]
    public void A_derived_kernel_with_no_definition_gets_the_definition_of_its_base_kernel() {
        Assert.That(Find([Vsa], OwnKernel, VsaKernel).Value, Is.EqualTo(Vsa.Type));
    }

    [Test]
    public void A_check_entry_of_the_app_block_names_the_definition() {
        var named = new TypeName("B.Other", "B");
        var entry = App with { Form = EntryForm.Metadata, Type = named, HasApp = true };

        Assert.That(Find([Vsa], VsaKernel, entry: entry).Value, Is.EqualTo(named));
    }

    [Test]
    public void An_app_and_a_definition_with_different_kernels_give_UMP030507() {
        var error = Find([Vsa, Own(OwnKernel)]).Error!;

        Assert.That(error.Code, Is.EqualTo("UMP030507"));
        Assert.That(error.Message, Does.Contain("A.App").And.Contain(VsaKernel.FullName)
            .And.Contain("A.MyArchitecture").And.Contain(OwnKernel.FullName));
    }

    [Test]
    public void A_definition_of_a_base_kernel_gives_UMP030507_if_the_kernel_has_its_own_definition() {
        var pack = new DefinitionType(new("B.Pack", "B"), Path.GetFullPath("C:/b"), OwnKernel, []);

        Assert.That(Find([Vsa, pack, Own()], OwnKernel, VsaKernel).Error!.Code, Is.EqualTo("UMP030507"));
    }

    [Test]
    public void A_definition_with_no_kernel_gives_UMP030507() {
        var error = Find([Vsa, Own() with { Kernel = null }]).Error!;

        Assert.That(error.Code, Is.EqualTo("UMP030507"));
        Assert.That(error.Message, Does.Contain("names no kernel"));
    }

    [Test]
    public void Two_definitions_in_the_Definition_folder_give_UMP030508() {
        var second = Own() with { Type = new("A.Second", "A") };

        Assert.That(Find([Vsa, Own(), second]).Error!.Message, Does.Contain("A.MyArchitecture, A.Second"));
        Assert.That(Find([Vsa, Own(), second]).Error!.Code, Is.EqualTo("UMP030508"));
    }

    [Test]
    public void The_most_derived_definition_in_the_Definition_folder_is_the_definition() {
        var leaf = new DefinitionType(new("A.Leaf", "A"), Path.GetFullPath("C:/a"), VsaKernel, [Own().Type.FullName, Vsa.Type.FullName]);

        Assert.That(Find([Vsa, Own(), leaf]).Value, Is.EqualTo(leaf.Type));
    }

    [Test]
    public void Two_definitions_of_one_kernel_give_UMP030508() {
        var second = Vsa with { Type = new("B.Second", "B") };

        Assert.That(Find([Vsa, second]).Error!.Code, Is.EqualTo("UMP030508"));
    }

    [Test]
    public void A_kernel_with_no_definition_gives_UMP030509() {
        Assert.That(Find([]).Error!.Code, Is.EqualTo("UMP030509"));
    }

    [Test]
    public void A_definition_of_the_block_outside_the_Definition_folder_gives_UMP030511() {
        var stray = Stray("A.ShopArchitecture", "C:/a/App/Host/ShopArchitecture.cs", VsaKernel);

        var error = Find([Vsa, stray]).Error!;

        Assert.That(error.Code, Is.EqualTo("UMP030511"));
        Assert.That(error.Message, Does.Contain("A.ShopArchitecture"));
    }

    [Test]
    public void A_base_of_the_used_definition_is_not_ignored() {
        var stray = Stray("A.Base", "C:/a/App/Base.cs", VsaKernel);
        var own = Own() with { Bases = ["A.Base", Vsa.Type.FullName], Source = Path.GetFullPath("C:/a/Definition/MyArchitecture.cs") };

        Assert.That(Find([Vsa, stray, own]).Value, Is.EqualTo(own.Type));
    }

    [TestCase(true, "UMP030511")]
    [TestCase(false, null)]
    public void The_definition_of_an_inner_pack_is_ignored_only_if_it_names_a_kernel_of_the_app(bool appKernel, string? code) {
        var folder = Path.GetFullPath("C:/a/App/Packs/X");
        var pack = Stray("X.XArchitecture", "C:/a/App/Packs/X/Definition/XArchitecture.cs", appKernel ? VsaKernel : OwnKernel);
        var entry = new BlockEntry(folder, EntryForm.Definition, new("A.PackCheck", "A"), folder, Packed: pack.Type);

        Assert.That(Find([Vsa, pack], inner: [entry]).Error?.Code, Is.EqualTo(code));
    }

    [TestCase(EntryForm.Metadata)]
    [TestCase(EntryForm.App)]
    [TestCase(EntryForm.Definition)]
    public void The_definition_of_an_inner_block_with_its_own_entry_is_not_ignored(EntryForm form) {
        var inner = Stray("T.ToolArchitecture", "C:/a/Tools/Definition/ToolArchitecture.cs", VsaKernel);
        var folder = Path.GetFullPath("C:/a/Tools");
        var entry = new BlockEntry(folder, form, inner.Type, folder, Packed: form == EntryForm.Definition ? inner.Type : null) {
            HasProject = true,
        };

        Assert.That(Find([Vsa, inner], inner: [entry]).Value, Is.EqualTo(Vsa.Type));
    }

    [Test]
    public void Source_files_in_the_Definition_folder_with_no_loaded_definition_give_UMP030512() {
        Assert.That(Find([Vsa], source: true).Error!.Code, Is.EqualTo("UMP030512"));
        Assert.That(Find([Vsa, Own()], source: true).Value, Is.EqualTo(Own().Type));
    }

    [Test]
    public void A_definition_that_did_not_load_stops_the_search_at_a_base_kernel() {
        var app = new AppClass(new("A.App", "A"), "A.cs", [OwnKernel], [VsaKernel], []);
        var dll = Unloaded("C:/a/App/Packs/Ecs/Definition/EcsArchitecture.cs") with { AppClasses = [app] };

        var error = DefinitionSearch.Find(App, [dll], [Vsa]).Error!;

        Assert.That(error.Code, Is.EqualTo("UMP030510"));
        Assert.That(error.Message, Does.Contain("assembly X").And.Not.Contain("project of the app"));
    }

    static DefinitionType Stray(string name, string source, TypeName kernel) =>
        new(new(name, "A"), DefinitionType.BlockFolder(Path.GetFullPath(source)), kernel, [Vsa.Type.FullName]) {
            Source = Path.GetFullPath(source),
        };

    static TypeName VsaKernel => new("Umpire.Vsa.Composition.VsaKernel", "Umpire.Vsa.Composition");

    static TypeName OwnKernel => new("A.MyKernel", "A");

    static DefinitionType Vsa => new(TypeName.Parse(VsaArchitecture)!, Path.GetFullPath("C:/vsa"), VsaKernel, []);

    static BlockEntry App => new(Path.GetFullPath("C:/a"), EntryForm.App, null, "C:/a");

    static DefinitionType Own(TypeName? kernel = null) =>
        new(new("A.MyArchitecture", "A"), Path.GetFullPath("C:/a"), kernel ?? VsaKernel, [Vsa.Type.FullName]);

    static Result<TypeName> Find(
        IReadOnlyList<DefinitionType> definitions, TypeName? kernel = null, TypeName? kernelBase = null, BlockEntry? entry = null,
        IReadOnlyList<BlockEntry>? inner = null, bool source = false) {
        var app = new AppClass(new("A.App", "A"), "A.cs", [kernel ?? VsaKernel], kernelBase is null ? [] : [kernelBase], []);
        var dll = new BinDll("A.dll", "A", false, [], [], [], [app]);
        return DefinitionSearch.Find(entry ?? App, [dll], definitions, inner, source);
    }

    [Test]
    public void An_app_class_that_cannot_load_gives_UMP030510_with_the_type_and_the_assembly() {
        var result = DefinitionSearch.Find(App, [Unloaded("C:/a/App/MyApp.cs")], [Vsa]);

        Assert.That(result.Error!.Code, Is.EqualTo("UMP030510"));
        Assert.That(result.Error.Message, Does.Contain("A.MyApp: the type X.Base did not load. Its assembly X is not in --bin"));
    }

    [Test]
    public void A_definition_that_cannot_load_gives_UMP030510() {
        var app = new AppClass(new("A.App", "A"), "A.cs", [VsaKernel], [], []);
        var dll = Unloaded("C:/a/Definition/MyArchitecture.cs") with { AppClasses = [app] };

        Assert.That(DefinitionSearch.Find(App, [dll], [Vsa]).Error!.Code, Is.EqualTo("UMP030510"));
    }

    [Test]
    public void A_definition_that_cannot_load_is_not_hidden_by_its_base() {
        var app = new AppClass(new("A.App", "A"), "A.cs", [VsaKernel], [], []);
        var dll = Unloaded("C:/a/Definition/MyArchitecture.cs") with { AppClasses = [app] };

        Assert.That(DefinitionSearch.Find(App, [dll], [Vsa, Own()]).Error!.Code, Is.EqualTo("UMP030510"));
    }

    [Test]
    public void A_definition_of_the_block_in_a_dll_that_is_not_an_app_dll_is_not_ignored() {
        var stray = Stray("T.TestArchitecture", "C:/a/App/TestArchitecture.cs", VsaKernel) with { Type = new("T.TestArchitecture", "T") };

        Assert.That(Find([Vsa, stray]).Value, Is.EqualTo(Vsa.Type));
    }

    [Test]
    public void A_type_that_cannot_load_does_not_hide_the_app_class() {
        var app = new AppClass(new("A.App", "A"), "A.cs", [VsaKernel], [], []);
        var dll = Unloaded("C:/a/App/Other.cs") with { AppClasses = [app] };

        Assert.That(DefinitionSearch.Find(App, [dll], [Vsa]).Value, Is.EqualTo(Vsa.Type));
    }

    [TestCase("ShellApp.Host.GameLauncher", "Umpire.Kernels", "ShellApp.dll", "Umpire.Kernels.IAppEntry`1")]
    [TestCase("UmpireEngine.EngineArchitecture", "Umpire.Vsa", "Engine.Definition.dll", "Umpire.Vsa.Definition.VsaArchitecture")]
    public void The_bin_names_the_type_and_the_assembly_that_did_not_load(
        string type, string assembly, string dll, string missing) {
        var bin = TempFolder.Create();
        try {
            foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "Umpire.*.dll")
                .Where(file => Path.GetFileNameWithoutExtension(file) != assembly)) {
                File.Copy(file, Path.Combine(bin, Path.GetFileName(file)));
            }
            foreach (var file in new[] { dll, Path.ChangeExtension(dll, ".pdb") }) {
                File.Copy(Path.Combine(AppContext.BaseDirectory, file), Path.Combine(bin, file));
            }

            using var reader = new BinReader(bin);
            var unloaded = reader.Index().Dlls.Single(d => d.Name == Path.GetFileNameWithoutExtension(dll)).Unloaded;

            var found = unloaded.Single(u => u.Type.FullName == type);
            Assert.That(found.MissingType, Is.EqualTo(missing));
            Assert.That(found.MissingAssembly, Is.EqualTo(assembly));
        } finally {
            TempFolder.Delete(bin);
        }
    }

    [TestCase("B", true)]
    [TestCase("A", false)]
    public void A_pack_that_holds_its_kernel_does_not_need_each_type_to_load(string kernelAssembly, bool isError) {
        var own = Own(new TypeName("A.MyKernel", kernelAssembly));
        var pack = App with { Form = EntryForm.Definition, Type = new("A.PackCheck", "A"), Packed = own.Type };

        Assert.That(DefinitionSearch.Find(pack, [Unloaded("C:/a/App/MyApp.cs")], [own]).IsSuccess, Is.EqualTo(!isError));
    }

    [Test]
    public void The_message_shows_the_classes_nearest_to_the_block_folder() {
        var far = Enumerable.Range(0, 9)
            .Select(i => new UnloadedType(new($"A.View{i}", "A"), $"C:/a/App/Features/Views/View{i}.cs", "X.Base", "X"));
        var dll = Unloaded("C:/a/App/MyApp.cs");

        var result = DefinitionSearch.Find(App, [dll with { Unloaded = [.. far, .. dll.Unloaded] }], [Vsa]);

        Assert.That(result.Error!.Message, Does.Contain("A.MyApp:"));
    }

    [Test]
    public void A_definition_of_an_inner_pack_that_cannot_load_does_not_stop_the_app() {
        var app = new AppClass(new("A.App", "A"), "A.cs", [VsaKernel], [], []);
        var dll = Unloaded("C:/a/App/Packs/Ecs/Definition/EcsArchitecture.cs") with { AppClasses = [app] };

        Assert.That(DefinitionSearch.Find(App, [dll], [Vsa]).Value, Is.EqualTo(Vsa.Type));
    }

    static BinDll Unloaded(string source) => new("A.dll", "A", false, [], [], [], []) {
        Unloaded = [new UnloadedType(new("A.MyApp", "A"), Path.GetFullPath(source), "X.Base", "X")],
    };

    [Test]
    public async Task The_dlls_to_check_have_each_shared_dll_that_the_block_references_with_no_external_name() {
        using var reader = new BinReader(AppContext.BaseDirectory);
        var bin = reader.Index();
        var kernel = Repo.Path("framework", "Kernel");
        var entry = bin.Entries.Single(e => Same(e.Folder, kernel) && e.Form == EntryForm.Metadata);
        var command = new LoadAppCommand(new BlockTree(entry, []), bin, "");
        using var services = new ServiceCollection().BuildServiceProvider();

        var input = (await new LoadAppHandler(new RunOptions()).HandleAsync(Envelope.For(command, services))).Value;

        Assert.That(new ResolvedArchitecture(input.Architecture).Parts.Where(p => p.IsExternal), Is.Empty);
        Assert.That(input.Dlls.Select(Path.GetFileNameWithoutExtension), Does.Contain("Umpire.Architecture"));
    }

    static bool Same(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(b),
            StringComparison.OrdinalIgnoreCase);
}

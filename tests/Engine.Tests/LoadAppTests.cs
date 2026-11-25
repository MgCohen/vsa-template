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

        var definition = LoadAppHandler.Definition(entry, [], [], new AppLoader(AppContext.BaseDirectory));

        Assert.That(new ResolvedArchitecture(definition.Value).Parts.Select(p => p.Path),
            Does.Contain("Kernel/Domain").And.Contain("Kernel/Handlers/Commands"));
    }

    [Test]
    public void A_pack_entry_with_a_block_architecture_that_cannot_take_it_gives_UMP030505() {
        var vsa = TypeName.Parse(VsaArchitecture);
        var entry = new BlockEntry(Path.GetFullPath("C:/a"), EntryForm.Definition, vsa, "C:/a/Definition/Pack.cs", Packed: vsa);

        var definition = LoadAppHandler.Definition(entry, [], [], new AppLoader(AppContext.BaseDirectory));

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

    static TypeName VsaKernel => new("Umpire.Vsa.Composition.VsaKernel", "Umpire.Vsa.Composition");

    static TypeName OwnKernel => new("A.MyKernel", "A");

    static DefinitionType Vsa => new(TypeName.Parse(VsaArchitecture)!, Path.GetFullPath("C:/vsa"), VsaKernel, []);

    static BlockEntry App => new(Path.GetFullPath("C:/a"), EntryForm.App, null, "C:/a");

    static DefinitionType Own(TypeName? kernel = null) =>
        new(new("A.MyArchitecture", "A"), Path.GetFullPath("C:/a"), kernel ?? VsaKernel, [Vsa.Type.FullName]);

    static Result<TypeName> Find(
        IReadOnlyList<DefinitionType> definitions, TypeName? kernel = null, TypeName? kernelBase = null, BlockEntry? entry = null) {
        var app = new AppClass(new("A.App", "A"), "A.cs", [kernel ?? VsaKernel], kernelBase is null ? [] : [kernelBase], []);
        var dll = new BinDll("A.dll", "A", false, [], [], [], [app]);
        return DefinitionSearch.Find(entry ?? App, [dll], definitions);
    }

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

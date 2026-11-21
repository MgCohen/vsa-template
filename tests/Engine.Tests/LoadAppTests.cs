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

        var definition = LoadAppHandler.Definition(entry, [], new AppLoader(AppContext.BaseDirectory));

        Assert.That(new ResolvedArchitecture(definition.Value).Parts.Select(p => p.Path),
            Does.Contain("Kernel/Domain").And.Contain("Kernel/Handlers/Commands"));
    }

    [Test]
    public void A_pack_entry_with_a_block_architecture_that_cannot_take_it_gives_UMP030505() {
        var vsa = TypeName.Parse(VsaArchitecture);
        var entry = new BlockEntry(Path.GetFullPath("C:/a"), EntryForm.Base, vsa, "C:/a/Pack.cs", Packed: vsa);

        var definition = LoadAppHandler.Definition(entry, [], new AppLoader(AppContext.BaseDirectory));

        Assert.That(definition.Error!.Code, Is.EqualTo("UMP030505"));
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

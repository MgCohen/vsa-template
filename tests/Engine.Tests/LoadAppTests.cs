using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using LoadApp.Slices.LoadApp;
using Tests.Shared;
using Umpire.Architecture;

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

    static bool Same(string a, string b) =>
        string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(a)), Path.TrimEndingDirectorySeparator(b),
            StringComparison.OrdinalIgnoreCase);
}

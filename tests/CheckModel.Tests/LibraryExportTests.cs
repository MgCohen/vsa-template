namespace CheckModel.Tests;

using Umpire.CheckModel;
using Umpire.CheckModel.Export;

public sealed class LibraryExportTests {
    [Test]
    public void A_library_type_has_its_bases_and_its_outer_type() {
        var bases = new[] { new TypeName("Kit.Base", "Kit"), new TypeName("Kit.IPart", "Kit") };

        var library = LibraryExport.Of("Kit", null, [new LibraryType("Kit.Outer+Inner", "/_/Kit/Inner.cs", 1, 1, bases)]);

        var type = library.Files.Single().Types.Single();
        Assert.That(type.Content.Bases.Select(b => b.FullName), Is.EqualTo(new[] { "Kit.Outer+Inner", "Kit.Base", "Kit.IPart" }));
        Assert.That(type.Location.DeclaringType, Is.EqualTo("Kit.Outer"));
    }
}

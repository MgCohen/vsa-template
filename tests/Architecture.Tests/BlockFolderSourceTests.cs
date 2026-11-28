using Tests.Shared;

namespace EngineCore.Tests;

using Facts.Infrastructure;
using Umpire.Architecture;

public sealed class BlockFolderSourceTests {
    [Test]
    public void A_group_place_gives_each_subfolder_in_order_and_a_missing_last_folder() {
        var block = TempFolder.Create();
        try {
            foreach (var folder in new[] { "b", "a", "obj", ".cache" }) {
                Directory.CreateDirectory(Path.Combine(block, "Boxes", folder));
            }
            Directory.CreateDirectory(Path.Combine(block, "Boxes", "b", "Tools"));

            var folders = new BlockFolderSource().Read(new BlockFolderQuery(block, ["Boxes/{Box}/Tools", "Kit"]));

            Assert.That(folders.Folders, Is.EqualTo(new[] {
                new PlaceFolder("Boxes/{Box}/Tools", "Boxes/a/Tools", false),
                new PlaceFolder("Boxes/{Box}/Tools", "Boxes/b/Tools", true),
                new PlaceFolder("Kit", "Kit", false),
            }));
        } finally {
            TempFolder.Delete(block);
        }
    }
}

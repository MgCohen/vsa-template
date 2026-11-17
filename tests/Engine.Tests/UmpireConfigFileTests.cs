using Tests.Shared;
using Umpire.Engine.Versions;

namespace Engine.Tests;

public sealed class UmpireConfigFileTests {
    [Test]
    public void The_parser_reads_the_top_level_keys_and_skips_comments_and_sections() {
        var config = UmpireConfigFile.Parse("x", [
            "# a comment",
            "; another comment",
            "umpire.architecture = A.B, A",
            "  umpire.UMP010101.max=140  ",
            "not a key line",
            "[*.cs]",
            "umpire.UMP010102.max = 1",
        ]);

        Assert.That(config.Values, Is.EqualTo(new Dictionary<string, string> {
            ["umpire.architecture"] = "A.B, A",
            ["umpire.UMP010101.max"] = "140",
        }));
        Assert.That(config.HasSections, Is.True);
        Assert.That(config["UMPIRE.ARCHITECTURE"], Is.EqualTo("A.B, A"));
        Assert.That(config["umpire.missing"], Is.Null);
    }

    [Test]
    public void The_nearest_file_is_at_or_above_the_folder() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, UmpireConfigFile.Name), "");
            var inner = Directory.CreateDirectory(Path.Combine(root, "a", "b")).FullName;

            Assert.That(UmpireConfigFile.Nearest(inner), Is.EqualTo(Path.Combine(Path.GetFullPath(root), UmpireConfigFile.Name)));
        } finally {
            TempFolder.Delete(root);
        }
    }
}

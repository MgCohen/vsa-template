using Tests.Shared;
using Umpire.Engine.Versions;

namespace Engine.Tests;

public sealed class BlockFolderTests {
    const string PinnedGlobalJson = """{ "msbuild-sdks": { "Umpire.Sdk": "0.0.1" } }""";

    [Test]
    public void The_declared_folder_wins_over_the_global_json() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, BlockFolder.Marker), PinnedGlobalJson);
            var declared = Directory.CreateDirectory(Path.Combine(root, "Apps", "Notes")).FullName;

            Assert.That(BlockFolder.Find(Path.Combine(declared, "App"), declared), Is.EqualTo(declared));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void With_no_declared_folder_the_block_is_the_nearest_folder_with_a_global_json() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, BlockFolder.Marker), "{}");
            var project = Directory.CreateDirectory(Path.Combine(root, "App", "Host")).FullName;

            Assert.That(BlockFolder.Find(project), Is.EqualTo(Path.GetFullPath(root)));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void The_tool_skips_a_global_json_with_no_Umpire_pin() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, BlockFolder.Marker), PinnedGlobalJson);
            var inner = Directory.CreateDirectory(Path.Combine(root, "Inner")).FullName;
            File.WriteAllText(Path.Combine(inner, BlockFolder.Marker), "{}");

            Assert.That(BlockFolder.Find(inner, needsUmpirePin: true), Is.EqualTo(Path.GetFullPath(root)));
            Assert.That(BlockFolder.Find(inner), Is.EqualTo(inner));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void The_not_found_message_gives_the_two_fixes() {
        var message = BlockFolder.NotFound("C:/apps/Notes");

        Assert.That(message, Does.Contain("Add a global.json file").And.Contain("--block").And.Contain("UmpireBlock"));
    }
}

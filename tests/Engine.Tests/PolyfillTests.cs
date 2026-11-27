using Tests.Shared;

namespace Engine.Tests;

using FolderSearch = Umpire.Engine.Polyfills.FolderSearch;
using PlatformPolyfills = Umpire.Engine.Polyfills.PlatformPolyfills;
using TextPolyfills = Umpire.Engine.Polyfills.TextPolyfills;

public sealed class PolyfillTests {
    [TestCase("a/b/")]
    [TestCase("a/b")]
    [TestCase("/")]
    [TestCase("")]
    [TestCase("a\\b\\")]
    public void TrimEndingDirectorySeparator_gives_the_path_of_the_runtime(string path) =>
        Assert.That(PlatformPolyfills.TrimEndingDirectorySeparator(path), Is.EqualTo(Path.TrimEndingDirectorySeparator(path)));

    [TestCase("Some Text text", "text", "X", StringComparison.Ordinal)]
    [TestCase("Some Text text", "text", "X", StringComparison.OrdinalIgnoreCase)]
    [TestCase("aaa", "a", "", StringComparison.Ordinal)]
    [TestCase("none", "z", "y", StringComparison.Ordinal)]
    public void Replace_gives_the_text_of_the_runtime(string text, string oldValue, string newValue, StringComparison comparison) =>
        Assert.That(TextPolyfills.Replace(text, oldValue, newValue, comparison),
            Is.EqualTo(text.Replace(oldValue, newValue, comparison)));

    [Test]
    public void Replace_of_an_empty_value_throws_as_the_runtime_does() =>
        Assert.Throws<ArgumentException>(() => TextPolyfills.Replace("text", "", "x", StringComparison.Ordinal));

    [Test]
    public void The_sha256_text_is_the_text_of_the_runtime() {
        byte[] bytes = [1, 2, 254, 255];

        Assert.That(PlatformPolyfills.Sha256Hex(bytes),
            Is.EqualTo(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))));
    }

    [TestCase(new[] { "build", "a b", "-p:X=1" }, "build \"a b\" -p:X=1")]
    [TestCase(new[] { "" }, "\"\"")]
    [TestCase(new[] { "say \"hi\"" }, "\"say \\\"hi\\\"\"")]
    [TestCase(new[] { "C:\\a b\\" }, "\"C:\\a b\\\\\"")]
    public void CommandLine_quotes_each_argument_as_the_runtime_parses_it(string[] arguments, string expected) =>
        Assert.That(PlatformPolyfills.CommandLine(arguments), Is.EqualTo(expected));

    [Test]
    public void MoveOver_replaces_the_destination() {
        var folder = TempFolder.Create();
        try {
            var source = Path.Combine(folder, "source");
            var destination = Path.Combine(folder, "destination");
            File.WriteAllText(source, "new");
            File.WriteAllText(destination, "old");

            PlatformPolyfills.MoveOver(source, destination);

            Assert.That(File.ReadAllText(destination), Is.EqualTo("new"));
            Assert.That(File.Exists(source), Is.False);
        } finally {
            TempFolder.Delete(folder);
        }
    }

    [Test]
    public void FolderSearch_finds_the_entries_of_the_runtime_search() {
        var folder = TempFolder.Create();
        try {
            Directory.CreateDirectory(Path.Combine(folder, "sub", "deep"));
            Directory.CreateDirectory(Path.Combine(folder, ".hidden"));
            foreach (var file in new[] { "a.cs", "b.csx", "sub/c.cs", "sub/deep/d.cs", ".hidden/e.cs", ".f.cs" }) {
                File.WriteAllText(Path.Combine(folder, file), "");
            }
            var runtime = new EnumerationOptions { RecurseSubdirectories = true };

            Assert.Multiple(() => {
                Assert.That(new FolderSearch { RecurseSubdirectories = true }.Files(folder, "*.cs").Order(),
                    Is.EqualTo(Directory.EnumerateFiles(folder, "*.cs", runtime).Order()));
                Assert.That(new FolderSearch().Folders(folder, "*").Order(),
                    Is.EqualTo(Directory.EnumerateDirectories(folder, "*", new EnumerationOptions()).Order()));
                Assert.That(new FolderSearch { AttributesToSkip = FileAttributes.System }.Files(folder, "*").Order(),
                    Is.EqualTo(Directory.EnumerateFiles(folder, "*", new EnumerationOptions { AttributesToSkip = FileAttributes.System })
                        .Order()));
            });
        } finally {
            TempFolder.Delete(folder);
        }
    }
}

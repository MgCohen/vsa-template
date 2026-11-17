using Tests.Shared;
using Umpire.Files;

namespace Cli.Tests;

public sealed class TextFileTests {
    string root = "";

    [SetUp]
    public void SetUp() => root = TempFolder.Create();

    [TearDown]
    public void TearDown() => TempFolder.Delete(root);

    [Test]
    public void ANewFileIsLfWithNoBom() {
        Assert.That(new TextFile("a/b.txt", "one\r\ntwo\n").Write(root), Is.True);

        Assert.That(File.ReadAllBytes(Path.Combine(root, "a", "b.txt")), Is.EqualTo("one\ntwo\n"u8.ToArray()));
    }

    [TestCase("x\r\ny\r\n", "x\r\ny\r\nz\r\n")]
    [TestCase("x\ny\n", "x\ny\nz\n")]
    [TestCase("x\r\ny\r\nw\n", "x\r\ny\r\nz\r\n")]
    [TestCase("x\r\ny\nw\n", "x\ny\nz\n")]
    [TestCase("x\r\ny\n", "x\ny\nz\n")]
    public void AChangedFileGetsItsDominantEnding(string before, string after) {
        File.WriteAllText(Path.Combine(root, "f.txt"), before);

        Assert.That(new TextFile("f.txt", "x\ny\nz\n").Write(root), Is.False);

        Assert.That(File.ReadAllText(Path.Combine(root, "f.txt")), Is.EqualTo(after));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AChangedFileKeepsItsBom(bool bom) {
        File.WriteAllText(Path.Combine(root, "f.txt"), "x\n", new System.Text.UTF8Encoding(bom));

        new TextFile("f.txt", "y\n").Write(root);

        Assert.That(File.ReadAllBytes(Path.Combine(root, "f.txt")) is [0xEF, 0xBB, 0xBF, ..], Is.EqualTo(bom));
    }

    [Test]
    public void WriteAllOrNoneWritesNothingIfOneFileExistsAndListsEachExistingFile() {
        File.WriteAllText(Path.Combine(root, "b.txt"), "mine");
        File.WriteAllText(Path.Combine(root, "c.txt"), "mine");

        Assert.That(() => TextFile.WriteAllOrNone(root, [new("a.txt", "new"), new("b.txt", "new"), new("c.txt", "new")]),
            Throws.TypeOf<FilesExistException>().With.Property("Paths").EqualTo(new[] { "b.txt", "c.txt" }));

        Assert.That(File.Exists(Path.Combine(root, "a.txt")), Is.False);
        Assert.That(File.ReadAllText(Path.Combine(root, "b.txt")), Is.EqualTo("mine"));
    }
}

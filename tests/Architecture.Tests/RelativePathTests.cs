namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class RelativePathTests {
    [TestCase("/a/b", "/a/b/c.cs")]
    [TestCase("/a/b", "/a/b")]
    [TestCase("/a/b/", "/a/b")]
    [TestCase("/a/b", "/a/b/")]
    [TestCase("/a/b/", "/a/b/")]
    [TestCase("/a/b", "/a")]
    [TestCase("/a/b", "/a/")]
    [TestCase("/a/b/c/", "/a/")]
    [TestCase("/a/b", "/x/y/")]
    [TestCase("/a/b", "/a/bc/d")]
    [TestCase("/a/bc", "/a/b/d")]
    [TestCase("/a/b", "/A/b/c")]
    [TestCase("/", "/a")]
    [TestCase("/a", "/")]
    [TestCase("/", "/")]
    [TestCase("/a/b/../c", "/a/c/d")]
    [TestCase("/a//b", "/a/b//c")]
    [TestCase("/a/b", "/a/b/c/d/")]
    [TestCase("/a/b/c", "/a/x")]
    [TestCase("rel/x", "rel/y/z")]
    public void The_relative_path_is_the_same_as_the_path_of_the_runtime(string relativeTo, string path) {
        var full = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.WorkDirectory, "Root"));
        var from = Path.Combine(full, relativeTo.TrimStart('/'));
        var to = Path.Combine(full, path.TrimStart('/'));
        Assert.That(RelativePath.Of(from, to), Is.EqualTo(Path.GetRelativePath(from, to)));
    }

    [Test]
    public void A_path_on_another_root_is_the_full_path() {
        var root = Path.GetPathRoot(Path.GetFullPath(TestContext.CurrentContext.WorkDirectory))!;
        if (root.Length <= 1) {
            Assert.Ignore("This platform has one root.");
        }
        var from = Path.Combine(root, "a");
        var other = root.StartsWith("Z", StringComparison.OrdinalIgnoreCase) ? @"Y:\x\y.cs" : @"Z:\x\y.cs";
        Assert.That(RelativePath.Of(from, other), Is.EqualTo(Path.GetRelativePath(from, other)));
    }
}

using Tests.Shared;

namespace Consumer.Tests;

public sealed class FeedTests {
    [Test]
    public void Each_package_in_the_feed_has_the_product_name() {
        Assert.That(LocalFeed.Packages, Has.All.StartsWith("Umpire."));
    }

    [Test]
    public void The_feed_has_the_tool_the_sdk_and_the_build_hook() {
        Assert.That(LocalFeed.Packages, Is.SupersetOf(new[] { "Umpire.Cli", "Umpire.Sdk", "Umpire.Engine.Build" }));
    }
}

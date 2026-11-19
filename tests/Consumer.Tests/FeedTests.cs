using Tests.Shared;

namespace Consumer.Tests;

public sealed class FeedTests {
    [Test]
    public void Each_package_in_the_feed_has_the_product_name() {
        Assert.That(LocalFeed.Packages, Has.All.StartsWith("Umpire."));
    }

    [Test]
    public void The_feed_has_the_tool_and_the_sdk_packages_with_no_engine_package() {
        Assert.That(LocalFeed.Packages, Is.SupersetOf(new[] { "Umpire.Cli", "Umpire.Sdk", "Umpire.Sdk.Web" }));
        Assert.That(LocalFeed.Packages, Has.No.Member("Umpire.Engine.Build"));
    }
}

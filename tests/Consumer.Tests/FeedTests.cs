using Tests.Shared;

namespace Consumer.Tests;

[Category("Package")]
public sealed class FeedTests {
    [Test]
    public void Each_package_in_the_feed_has_the_product_name() {
        Assert.That(LocalFeed.Packages, Has.All.Matches<string>(id => id == "Umpire" || id.StartsWith("Umpire.", StringComparison.Ordinal)));
    }

    [Test]
    public void The_feed_has_the_tool_and_the_engine_package_and_no_sdk_package() {
        Assert.That(LocalFeed.Packages, Is.SupersetOf(new[] { "Umpire.Cli", "Umpire", "Umpire.Engine" }));
        Assert.That(LocalFeed.Packages, Has.No.Member("Umpire.Sdk").And.No.Member("Umpire.Sdk.Web").And.No.Member("Umpire.Engine.Build"));
    }
}

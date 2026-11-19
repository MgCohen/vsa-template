using System.IO.Compression;
using Tests.Shared;

namespace Consumer.Tests;

public sealed class PortablePackageTests {
    [TestCase("Umpire")]
    public void A_package_that_the_app_needs_in_Unity_has_a_netstandard2_1_library(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));

        Assert.That(nupkg.Entries.Select(e => e.FullName), Does.Contain($"lib/netstandard2.1/{package}.dll"));
    }
}

using System.IO.Compression;
using Tests.Shared;

namespace Consumer.Tests;

[Category("Package")]
public sealed class PortablePackageTests {
    [TestCase("Umpire")]
    [TestCase("Umpire.Vsa")]
    [TestCase("Umpire.Definition")]
    [TestCase("Umpire.Vsa.Definition")]
    public void A_package_that_the_app_needs_in_Unity_has_a_netstandard2_0_library(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));

        Assert.That(nupkg.Entries.Select(e => e.FullName), Does.Contain($"lib/netstandard2.0/{package}.dll"));
    }
}

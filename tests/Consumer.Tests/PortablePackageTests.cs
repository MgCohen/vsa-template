using System.IO.Compression;
using Tests.Shared;

namespace Consumer.Tests;

public sealed class PortablePackageTests {
    [TestCase("VSA.Composition")]
    [TestCase("VSA.Architecture")]
    [TestCase("VSA.Architecture.Vsa")]
    [TestCase("VSA.Kernel")]
    [TestCase("VSA.Module")]
    public void A_package_that_VsaApp_needs_in_Unity_has_a_netstandard2_1_library(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));

        Assert.That(nupkg.Entries.Select(e => e.FullName), Does.Contain($"lib/netstandard2.1/{package}.dll"));
    }
}

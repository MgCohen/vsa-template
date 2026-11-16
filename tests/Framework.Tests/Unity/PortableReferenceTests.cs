namespace Framework.Tests.Unity;

public sealed class PortableReferenceTests {
    const int UnityExtensionsMajorVersion = 8;

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Names))]
    public void A_portable_project_uses_the_Microsoft_Extensions_version_of_Unity(string project) {
        var extensions = BuiltAssembly.OfMultiTarget(project, "netstandard2.1").References
            .Where(reference => reference.Name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal));

        Assert.That(extensions.Select(reference => reference.Version.Major), Has.All.AtMost(UnityExtensionsMajorVersion));
    }
}

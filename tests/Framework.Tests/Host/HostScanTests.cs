using VSA.Composition;
using VSA.Host;

namespace Framework.Tests.Host;

public sealed class HostScanTests {
    [Test]
    public void The_scan_selects_the_web_host_when_VSA_Host_is_loaded() {
        var host = VsaHosts.Find([typeof(VsaWebHostExtensions).Assembly]);

        Assert.That(host.GetType().Name, Is.EqualTo("WebVsaHost"));
    }

    [Test]
    public void The_scan_selects_a_host_that_another_package_plugs_in() {
        var host = VsaHosts.Find([typeof(ReturningHost).Assembly]);

        Assert.That(host, Is.TypeOf<ReturningHost>());
    }

    [Test]
    public void The_scan_selects_the_generic_host_when_no_assembly_has_a_host() {
        var host = VsaHosts.Find([typeof(VSA.Module.Module).Assembly]);

        Assert.That(host.GetType().Name, Is.EqualTo("GenericVsaHost"));
    }

    [Test]
    public void Two_hosts_are_an_error_that_names_them() {
        var error = Assert.Throws<InvalidOperationException>(
            () => VsaHosts.Find([typeof(VsaWebHostExtensions).Assembly, typeof(ReturningHost).Assembly]));

        Assert.That(error!.Message, Is.EqualTo(
            "The app has 2 hosts: Framework.Tests.Host.ReturningHost, VSA.Host.WebVsaHost. "
            + "Remove the reference to one host, or select one in the Builder step of the app: VsaBuilder.Create(args).UseHost(...)."));
    }
}

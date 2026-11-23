namespace SampleApp.Tests;

using SampleApp.Host;
using Umpire.Architecture;
using Umpire.Vsa.Testing.Unit;

public sealed class SampleAppArchitectureTests : VsaArchitectureTest {
    [Test]
    public void The_app_class_binds_to_the_app_host() {
        Assert.That(At<SampleAppApp>("Host").Part, Is.EqualTo("Host"));
    }

    [Test]
    public void A_view_does_not_use_the_infrastructure_of_a_module() {
        var view = At("Views");
        var infrastructure = At(Feature("Example") + "/Infrastructure");

        Assert.That(MayUse(view, infrastructure).Permission, Is.EqualTo(Permission.Deny));
    }
}

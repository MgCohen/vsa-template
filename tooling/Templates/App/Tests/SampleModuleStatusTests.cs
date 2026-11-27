namespace SampleApp.Tests;

using SampleApp.Features.SampleModule.Slices.GetStatus;
using Umpire.Vsa.Testing.Unit;

public sealed class SampleModuleStatusTests : VsaUnitTest {
    [Test]
    public async Task The_module_is_ready() {
        var result = await Handle(new GetStatusRequest());

        Assert.That(result.Value, Is.EqualTo(new GetStatusResponse("SampleModule", "Ready")));
    }
}

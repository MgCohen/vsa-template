using SampleModule.Slices.GetStatus;
using VSA.Testing.Unit;

namespace App.Tests;

// A unit test of one slice: the request goes to its handler, with the default fakes.
public sealed class SampleModuleStatusTests : VsaUnitTest {
    [Test]
    public async Task The_module_is_ready() {
        var result = await Handle(new GetStatusRequest());

        Assert.That(result.Value, Is.EqualTo(new GetStatusResponse("SampleModule", "Ready")));
    }
}

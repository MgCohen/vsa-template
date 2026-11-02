using Good.Contracts.Requests;
using VSA.Testing.Unit;

namespace Rules.Tests;

// The unit test base on the Good fixture: it finds the handler of a request in the module and runs it.
public sealed class UnitBaseTests : VsaUnitTest {
    [Test]
    public async Task Handle_runs_the_handler_of_the_request() {
        var id = Guid.NewGuid();
        var result = await Handle(new GetItemRequest(id));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(new GetItemResponse(id)));
    }

    [Test]
    public void Each_test_gets_new_default_fakes() => Assert.That(Events.Published, Is.Empty);
}

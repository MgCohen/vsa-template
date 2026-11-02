using Good.Contracts.Requests;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using VSA.Contracts.Events;
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

    [Test]
    public async Task A_service_added_after_Handle_fails() {
        await Handle(new GetItemRequest(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => Mock<IClock>());
    }
}

// Configure runs before each test, so each test gets a new mock in the field.
public sealed class UnitBaseConfigureTests : VsaUnitTest {
    IClock clock = null!;
    IIntegrationEventPublisher publisher = null!;

    protected override void Configure(IServiceCollection services) {
        clock = Mock<IClock>();
        clock.Now.Returns(42);
        publisher = Mock<IIntegrationEventPublisher>();
    }

    [Test]
    public void The_services_give_the_mock() {
        Assert.That(Provider.GetRequiredService<IClock>().Now, Is.EqualTo(42));
        clock.Now.Returns(7);
    }

    [Test]
    public void Each_test_gets_a_new_mock() => Assert.That(clock.Now, Is.EqualTo(42));

    // A mock replaces a default fake: the last registration wins.
    [Test]
    public void A_mock_replaces_a_default_fake() =>
        Assert.That(Provider.GetRequiredService<IIntegrationEventPublisher>(), Is.SameAs(publisher));
}

public interface IClock {
    int Now { get; }
}

using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Resources.Domain.Shared;
using Resources.Slices.ChangeResource;
using VSA.Contracts.Commands;
using VSA.Testing.Unit;

namespace Rules.Tests;

// The unit test base runs a command handler. The handler sends a command of Resources, so the test mocks ICommandSender.
public sealed class UnitBaseCommandTests : VsaUnitTest {
    ICommandSender commands = null!;

    protected override void Configure(IServiceCollection services) {
        commands = Mock<ICommandSender>();
        commands.SendAsync(Arg.Any<ChangeResourceCommand>(), Arg.Any<CancellationToken>()).Returns(new ChangeResourceResult(true, 7));
    }

    [Test]
    public async Task Send_runs_the_handler_of_the_command() {
        var cost = new Cost(ResourceKeys.Mana.Name, 3);

        var result = await Send(new PayCostCommand(cost));

        Assert.That(result.Value, Is.EqualTo(new PayCostResult(true, 7)));
        Assert.That(Events.OfType<CostPaid>().Single().Cost, Is.EqualTo(cost));
        await commands.Received(1).SendAsync(new ChangeResourceCommand(ResourceKeys.Mana, -3), Arg.Any<CancellationToken>());
    }
}

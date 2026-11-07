using System.Net.Http.Json;
using Battle;
using Battle.Slices.PlayCard;
using Cards;
using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Echo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Resources;
using Resources.Domain.Shared;
using VSA.Contracts.Commands;
using VSA.App;
using VSA.Host;

namespace Framework.Tests.Commands;

// The host registers ICommandSender and the one handler of each command (tests/Fixtures/CommandApp).
// Battle PlayCard sends PayCost to Cards. Cards sends ChangeResource to Resources. Each mana pool starts at 10.
public sealed class CommandTests {
    [Test]
    public async Task A_request_handler_sends_commands_through_two_modules() {
        await using var app = await Start(new BattleModule(), new CardsModule(), new ResourcesModule());
        var client = app.GetTestClient();

        var played = await client.PostAsJsonAsync("/api/cards/play", new PlayCardRequest(4));
        var tooMuch = await client.PostAsJsonAsync("/api/cards/play", new PlayCardRequest(7));

        Assert.That(await played.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PlayCardResponse>(), Is.EqualTo(new PlayCardResponse(true, 6)));
        Assert.That(await tooMuch.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PlayCardResponse>(), Is.EqualTo(new PlayCardResponse(false, 6)));
    }

    [Test]
    public async Task The_sender_runs_the_handler_in_the_scope_of_the_caller() {
        await using var app = await Start(new CardsModule(), new ResourcesModule());
        await using var scope = app.Services.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default);

        Assert.That(result, Is.EqualTo(new PayCostResult(true, 7)));
    }

    // Cards sends a command of Resources, but the app has no Resources module.
    [Test]
    public async Task A_command_with_no_handler_is_an_error() {
        await using var app = await Start(new CardsModule());
        await using var scope = app.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendAsync(new PayCostCommand(new Cost(ResourceKeys.Mana, 3)), default));

        Assert.That(error!.Message, Does.StartWith("Command Resources.Slices.ChangeResource.ChangeResourceCommand has no handler."));
    }

    [Test]
    public void A_command_with_two_handlers_stops_start_up() {
        var builder = WebApplication.CreateBuilder();

        var error = Assert.Throws<InvalidOperationException>(() => builder.AddVsa(new EchoModule()));

        Assert.That(error.Message, Does.StartWith("Command Echo.Slices.Echo.EchoCommand has two handlers:").And.EndWith("A command has one handler."));
    }

    static async Task<WebApplication> Start(params VSA.Module.Module[] modules) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddVsa(modules);
        var app = builder.Build();
        app.MapVsaRequests();
        await app.StartAsync();
        return app;
    }
}

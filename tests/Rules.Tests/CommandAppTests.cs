using System.Text.Json.Nodes;
using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Resources.Domain.Shared;
using Resources.Slices.ChangeResource;
using Tests.Shared;
using VSA.Contracts.Commands;
using VSA.Testing.Architecture;
using VSA.Testing.Unit;
using static Rules.Tests.Fixtures;

namespace Rules.Tests;

// The references of VSA.Sdk for commands and Domain.Shared, on tests/Fixtures/CommandApp.
// Resources has a Domain.Shared project and a command. Cards has a Domain.Shared project and a command,
// an event and a presenter. Battle has a request handler that sends the command of Cards.
public sealed class SdkCommandTests {
    static string App(params string[] parts) => Repo.Path(["tests", "Fixtures", "CommandApp", .. parts]);

    static string Layer(string module, string layer) => App("Modules", module, "Projects", $"{module}.{layer}.csproj");

    static string Root(string module) => App("Modules", module, $"{module}.csproj");

    // The project references that VSA.Sdk gives, as paths in the fixture app, for example "Cards/Cards.Domain.Shared".
    static IReadOnlyList<string> References(string project, params string[] properties) {
        var result = Dotnet.Run(Repo.Root, null, ["msbuild", project, "-getItem:ProjectReference", .. properties]).EnsureSuccess();
        var root = App("Modules").Replace('\\', '/') + "/";
        return [.. JsonNode.Parse(result.Output)!["Items"]!["ProjectReference"]!.AsArray()
            .Select(item => ((string)item!["Identity"]!).Replace('\\', '/'))
            .Where(path => path.StartsWith(root, StringComparison.Ordinal))
            .Select(path => path[root.Length..].Replace("/Projects/", "/", StringComparison.Ordinal).Replace(".csproj", "", StringComparison.Ordinal))];
    }

    static string Fails(string project, string target, params string[] properties) {
        var result = Dotnet.Run(Repo.Root, null, ["msbuild", project, $"-t:{target}", "-nologo", .. properties]);
        Assert.That(result.Succeeded, Is.False, result.Output);
        return result.Output;
    }

    [TestCase("Cards")]
    [TestCase("Resources")]
    public void A_Domain_Shared_project_gets_no_other_module(string module) =>
        Assert.That(References(Layer(module, "Domain.Shared")), Is.Empty);

    [Test]
    public void Domain_gets_its_own_Domain_Shared_only() =>
        Assert.That(References(Layer("Resources", "Domain")), Is.EquivalentTo(new[] { "Resources/Resources.Domain.Shared" }));

    [Test]
    public void A_presenter_gets_its_own_events_and_the_Domain_Shared_of_all_modules() =>
        Assert.That(References(Layer("Cards", "Presenters")), Is.EquivalentTo(new[] {
            "Cards/Cards.Contracts.Events",
            "Cards/Cards.Domain.Shared",
            "Resources/Resources.Domain.Shared",
        }));

    [Test]
    public void A_contracts_project_gets_the_Domain_Shared_of_all_modules() =>
        Assert.That(References(Layer("Resources", "Contracts.Commands")), Is.EquivalentTo(new[] {
            "Cards/Cards.Domain.Shared",
            "Resources/Resources.Domain.Shared",
        }));

    [TestCase("Battle", "Handlers.Requests")]
    [TestCase("Cards", "Handlers.Commands")]
    public void A_handler_gets_the_commands_and_the_Domain_Shared_of_all_modules(string module, string layer) =>
        Assert.That(References(Layer(module, layer)), Is.SupersetOf(new[] {
            "Cards/Cards.Contracts.Commands",
            "Cards/Cards.Domain.Shared",
            "Resources/Resources.Contracts.Commands",
            "Resources/Resources.Domain.Shared",
        }));

    [Test]
    public void A_command_handler_gets_its_own_events() =>
        Assert.That(References(Layer("Cards", "Handlers.Commands")), Does.Contain("Cards/Cards.Contracts.Events"));
}

public sealed class CommandAppRuleTests {
    static VsaApp App() => new([
        Root("Battle"),
        Root("Cards"),
        Root("Resources"),
    ]);

    [Test]
    public void The_fixture_follows_the_layer_map() => FixtureApp.Of(App()).VSA2001_module_projects_follow_the_layer_map();
}

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

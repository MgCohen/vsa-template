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

// The references of VSA.Sdk for commands, Domain.Shared and VsaModuleUses, on tests/Fixtures/CommandApp.
// Resources has a Domain.Shared project and a command. Cards lists Resources in VsaModuleUses, and has a command,
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

    [Test]
    public void A_Domain_Shared_project_gets_the_Domain_Shared_of_the_listed_modules() =>
        Assert.That(References(Layer("Cards", "Domain.Shared")), Is.EquivalentTo(new[] { "Resources/Resources.Domain.Shared" }));

    [Test]
    public void A_Domain_Shared_project_with_no_list_gets_no_module() =>
        Assert.That(References(Layer("Resources", "Domain.Shared")), Is.Empty);

    [Test]
    public void Domain_gets_its_own_Domain_Shared_only() =>
        Assert.That(References(Layer("Resources", "Domain")), Is.EquivalentTo(new[] { "Resources/Resources.Domain.Shared" }));

    [Test]
    public void A_presenter_gets_its_own_events_and_the_Domain_Shared_of_the_listed_modules() =>
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

    [TestCase("Missing", Description = "no module with this name")]
    [TestCase("Cards", Description = "the module itself")]
    [TestCase("Battle", Description = "a module with no Domain.Shared project")]
    [TestCase("../Resources", Description = "a path, not a name")]
    public void A_bad_name_in_VsaModuleUses_fails(string name) =>
        Assert.That(Fails(Root("Cards"), "_VsaCheckModuleUses", $"-p:VsaModuleUses={name}"),
            Does.Contain("VSA0007").And.Contain("are not other modules with a Domain.Shared project"));

    // The root gets the list from MSBuild, and the layer projects read the text of the file. A value from elsewhere is an error.
    [Test]
    public void A_VsaModuleUses_value_that_is_not_the_text_of_the_root_fails() =>
        Assert.That(Fails(Root("Cards"), "_VsaCheckModuleUses", "-p:VsaModuleUses=Resources%3BResources"),
            Does.Contain("VSA0007").And.Contain("but its file has the text 'Resources'"));

    // A global property is also set in each layer project, as if the layer project set it.
    [Test]
    public void VsaModuleUses_in_a_layer_project_fails() =>
        Assert.That(Fails(Layer("Cards", "Presenters"), "_VsaCheckModuleUses", "-p:VsaModuleUses=Resources"),
            Does.Contain("VSA0007").And.Contain("Set VsaModuleUses in the module root"));

    [Test]
    public void The_module_root_records_the_listed_modules() {
        Assert.That(Fixtures.Root("Cards").Uses, Is.EqualTo(new[] { "Resources" }));
        Assert.That(Fixtures.Root("Resources").Uses, Is.Empty);
    }
}

public sealed class CommandAppRuleTests {
    static VsaApp App(params string[] cardsUses) => new([
        Root("Battle"),
        Root("Cards") with { Uses = cardsUses },
        Root("Resources"),
    ]);

    [Test]
    public void The_fixture_follows_the_layer_map() => FixtureApp.Of(App("Resources")).VSA2001_module_projects_follow_the_layer_map();

    // With no list, Cost (Cards.Domain.Shared) and CostLabel (Cards.Presenters) use a module that Cards does not list.
    [Test]
    public void Use_of_the_Domain_Shared_of_a_module_that_is_not_listed_fails() {
        var message = Fails(FixtureApp.Of(App()).VSA2001_module_projects_follow_the_layer_map);
        Assert.That(message, Does.Contain("Cards.Domain.Shared.Cost does depend on \"Resources.Domain.Shared.ResourceKey\""));
        Assert.That(message, Does.Contain("Cards.Presenters.CostLabel"));
    }
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
        var cost = new Cost(ResourceKeys.Mana, 3);

        var result = await Send(new PayCostCommand(cost));

        Assert.That(result.Value, Is.EqualTo(new PayCostResult(true, 7)));
        Assert.That(Events.OfType<CostPaid>().Single().Cost, Is.EqualTo(cost));
        await commands.Received(1).SendAsync(new ChangeResourceCommand(ResourceKeys.Mana, -3), Arg.Any<CancellationToken>());
    }
}

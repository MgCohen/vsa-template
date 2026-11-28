namespace EngineCore.Tests;

using Umpire.Vsa.Definition;

public sealed class VsaAppTests {
    const string Notebook = "VsaApp/App/Features/Notebook/";
    const string Shop = "VsaApp/App/Features/Shop/";

    static readonly Lazy<CheckedApp> VsaMap =
        new(() => FixtureApps.Load(new VsaArchitecture(), AppContext.BaseDirectory, FixtureApps.Block("VsaApp")));

    static readonly string[] Known = [
        "VsaApp/App/Host/LeakyHost.cs(7): error UMP020101",
        Notebook + "Contracts/StrayContract.cs(3): error UMP020202",
        Notebook + "Slices/Archive/ArchiveAuditHook.cs(7): error UMP020301",
        Notebook + "Slices/Count/CountRequest.cs(6): error UMP020301",
        Notebook + "Slices/Count/CountResponse.cs(5): error UMP020301",
        Notebook + "Slices/Draft/DraftNote.cs(3): error UMP020301",
        Notebook + "Slices/LooseRecord.cs(3): error UMP020202",
        Notebook + "Slices/Peek/PeekRequest.cs(8): error UMP020101",
        Notebook + "Slices/Ping/PingRequest.cs(6): error UMP020301",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020302",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020302",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020101",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020101",
        "VsaApp/App/Features/Rogue/RoguePlugin.cs(1): error UMP020101",
        Shop + "Domain/Shared/Price.cs(5): error UMP020303",
        Shop + "Domain/Shared/Price.cs(16): error UMP020303",
        Shop + "Domain/Shared/Slot.cs(3): error UMP020303",
        Shop + "Infrastructure/AuditHubStep.cs(8): error UMP020301",
        Shop + "Presenters/LazyPresenter.cs(8): error UMP020101",
        Shop + "Presenters/PipelinePresenter.cs(7): error UMP020101",
        Shop + "Presenters/PublishPresenter.cs(9): error UMP020101",
        Shop + "Slices/LogSale/LazySellHandler.cs(8): error UMP020101",
        Shop + "Slices/LogSale/ResellHandler.cs(10): error UMP020101",
        Shop + "Slices/LogSale/ResellHandler.cs(10): error UMP020101",
        Shop + "Slices/LogSale/ResellHandler.cs(10): error UMP020101",
        Shop + "Slices/Sell/SellRequest.cs(7): error UMP020303",
        Shop + "Slices/Sell/SellResponse.cs(1): error UMP020303",
        "VsaApp/App/Features/Twin/TwinExtraModule.cs(1): error UMP020301",
        "VsaApp/App/Features/Twin/TwinExtraModule.cs(1): error UMP020301",
        "VsaApp/App/Features/Helped/HelpedModule.cs(1): error UMP020301",
        "VsaApp/App/Plugins/Stray/StrayModule.cs(1): error UMP020301",
        "VsaApp/App/Plugins/Tools/Infrastructure/NoteReader.cs(6): error UMP020101",
        "VsaApp/App/Plugins/Tools/Slices/Run/RunStep.cs(3): error UMP020202",
    ];

    static IReadOnlyList<string> Findings => [.. VsaMap.Value.Findings().Select(FixtureApps.Line)];

    [Test]
    public void A_host_that_names_a_module_root_in_a_fixed_list_gives_no_finding() =>
        Assert.That(Findings, Has.None.Contains("App/Host/FixedHost.cs"));

    [Test]
    public void A_host_that_uses_the_domain_of_a_feature_gives_UMP020101() =>
        Assert.That(Findings,
            Has.One.StartsWith("VsaApp/App/Host/LeakyHost.cs(7): error UMP020101: LeakyHost in Host (App/Host) uses Note in Domain"));

    [TestCase(Notebook + "Contracts/StrayContract.cs", "UMP020202", "StrayContract is in a folder")]
    [TestCase("VsaApp/App/Plugins/Tools/Slices/Run/RunStep.cs", "UMP020202", "RunStep is in a folder")]
    [TestCase(Notebook + "Slices/LooseRecord.cs", "UMP020202", "LooseRecord is in a folder")]
    [TestCase(Notebook + "Slices/Peek/PeekRequest.cs", "UMP020101", "PeekRequest in Contracts.Requests")]
    [TestCase(Notebook + "Slices/Draft/DraftNote.cs", "UMP020301", "Slice App/Features/Notebook/Slices/Draft has 0 Handlers types")]
    [TestCase(Notebook + "Slices/Archive/ArchiveAuditHook.cs", "UMP020301", "has Handlers types of 2 kinds")]
    [TestCase(Notebook + "Slices/Count/CountRequest.cs", "UMP020301", "has 2 Handlers.Requests types that take it")]
    [TestCase(Notebook + "Slices/Pong/PongHandler.cs", "UMP020302", "takes PingRequest in Contracts.Requests")]
    [TestCase("VsaApp/App/Features/Twin/TwinExtraModule.cs", "UMP020301", "Feature App/Features/Twin has 2 types of Module")]
    [TestCase("VsaApp/App/Features/Helped/HelpedModule.cs", "UMP020301", "Feature App/Features/Helped has 2 types (HelpedModule, Helper)")]
    [TestCase("VsaApp/App/Features/Rogue/RoguePlugin.cs", "UMP020101", "the rule \"Feature(Self) denies Composition\" denies it")]
    [TestCase("VsaApp/App/Plugins/Stray/StrayModule.cs", "UMP020301", "Plugin App/Plugins/Stray has 0 types of Plugin")]
    [TestCase("VsaApp/App/Plugins/Tools/Infrastructure/NoteReader.cs", "UMP020101", "the rule \"Plugin denies Feature\" denies it")]
    [TestCase(Shop + "Slices/LogSale/LazySellHandler.cs", "UMP020101", "uses IRequestSender in Contracts.Requests (Umpire.Vsa.Contracts.Requests.dll): the rule \"Handlers.Events denies Contracts.Requests\"")]
    [TestCase(Shop + "Slices/LogSale/ResellHandler.cs", "UMP020101", "uses IRequestSender in Contracts.Requests")]
    [TestCase(Shop + "Slices/LogSale/ResellHandler.cs", "UMP020101", "uses SellRequest in Contracts.Requests")]
    [TestCase(Shop + "Presenters/PublishPresenter.cs", "UMP020101", "the rule \"Presenters denies Contracts.Events.Publishing Under<Kernel>\" denies it")]
    [TestCase(Shop + "Presenters/LazyPresenter.cs", "UMP020101", "uses IIntegrationEventPublisher in Contracts.Events.Publishing")]
    [TestCase(Shop + "Presenters/PipelinePresenter.cs", "UMP020101", "uses Pipelines in KernelParts.Messages")]
    [TestCase(Shop + "Infrastructure/AuditHubStep.cs", "UMP020301", "has 1 types of HubStep (AuditHubStep)")]
    [TestCase(Shop + "Infrastructure/AuditHubStep.cs", "UMP020301", "Remove AuditHubStep, or move each one to a part where the rule does not apply.")]
    [TestCase(Shop + "Slices/Sell/SellRequest.cs", "UMP020303", "it is not record or abstract")]
    [TestCase(Shop + "Slices/Sell/SellResponse.cs", "UMP020303", "it is not record or abstract")]
    [TestCase(Shop + "Domain/Shared/Price.cs", "UMP020303", "Price is in Domain.Shared (App/Features/Shop/Domain/Shared), but it is not immutable (Count, Changed, Amount can change)")]
    [TestCase(Shop + "Domain/Shared/Price.cs", "UMP020303", "Tag is in Domain.Shared (App/Features/Shop/Domain/Shared), but it is not immutable (Text can change)")]
    [TestCase(Shop + "Domain/Shared/Slot.cs", "UMP020303", "it is not immutable (Index can change)")]
    public void The_VSA_app_gives_the_finding_of_each_case(string file, string code, string text) {
        Assert.That(Findings, Has.Some.StartsWith(file).And.Contains($"error {code}: ").And.Contains(text));
    }

    [TestCase(Notebook + "Slices/Shared/NoteTitle.cs")]
    [TestCase(Notebook + "Slices/GetNote/")]
    [TestCase(Notebook + "Slices/ReadNote/")]
    [TestCase("VsaApp/App/Plugins/Tools/Infrastructure/ToolClock.cs")]
    [TestCase("VsaApp/App/Plugins/Clock/")]
    [TestCase("VsaApp/App/Plugins/Clock/Infrastructure/ClockHubStep.cs")]
    [TestCase(Shop + "Domain/Shared/Amount.cs")]
    [TestCase(Shop + "Domain/Shared/Discount.cs")]
    [TestCase(Shop + "Presenters/ShopPresenter.cs")]
    [TestCase(Shop + "Slices/LogSale/LogSaleHandler.cs")]
    [TestCase(Shop + "Slices/Refund/")]
    [TestCase(Shop + "Slices/Sell/SellHandler.cs")]
    public void A_file_that_follows_the_rules_gives_no_finding(string file) {
        Assert.That(Findings, Has.None.StartsWith(file));
    }

    [TestCase("GetNoteValidator", "Slice.Validators")]
    [TestCase("TitleCheck", "Handlers.Requests")]
    [TestCase("ShortTitleCheck", "Handlers.Requests")]
    public void A_type_in_a_slice_binds_to_its_part(string type, string part) {
        var placement = VsaMap.Value.Model.PlacementOf(VsaMap.Value.Named(type))!;

        Assert.That(placement.Part.Name, Is.EqualTo(part));
    }

    [Test]
    public void A_settings_module_with_a_nested_type_and_a_lambda_gives_no_count_finding() {
        Assert.Multiple(() => {
            Assert.That(VsaMap.Value.PlaceOf("VsaApp.Features.Settled.SettledModule")?.Part.Name, Is.EqualTo("Feature"));
            Assert.That(Findings, Has.None.Contains("Features/Settled/"));
        });
    }

    [Test]
    public void The_VSA_app_gives_only_the_known_findings() {
        var actual = Findings.Select(line => line[..(line.IndexOf("error ", StringComparison.Ordinal) + "error UMP000000".Length)]);

        Assert.That(actual, Is.EquivalentTo(Known));
    }
}

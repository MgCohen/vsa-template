using VSA.Architecture.Engine;

namespace Architecture.Tests;

public sealed class BadAppTests {
    const string Billing = "BadApp/Features/Billing";
    const string Notes = "BadApp/Features/Notes";

    static readonly Lazy<IReadOnlyList<RuleFinding>> AllFindings = new(() => FixtureApps.Bad.Findings());

    static string UseFailures => Lines("VSA2001");

    static string ExposureFailures => Lines("VSA2003");

    [Test]
    public void The_bad_app_has_only_the_known_findings() {
        Assert.That(AllFindings.Value, Has.Count.EqualTo(12), Lines(null));
    }

    [Test]
    public void A_finding_gives_the_full_path_of_its_file_and_its_line() {
        var finding = AllFindings.Value.Single(f => f.Code == "VSA2102");

        Assert.That(finding.File, Is.EqualTo(Path.Combine(FixtureApps.Root, "BadApp", "Features", "Billing", "Utils", "Clock.cs")));
        Assert.That((finding.Severity, finding.Line), Is.EqualTo((RuleSeverity.Error, 4)));
    }

    [Test]
    public void A_use_that_no_rule_allows_gives_VSA2001_with_the_file_and_line() {
        Assert.That(UseFailures, Does.Contain(
            $"{Notes}/Slices/ReadInvoice/ReadInvoiceHandler.cs(6): error VSA2001: ReadInvoiceHandler in Handlers.Requests "
            + $"({Notes}/Slices/ReadInvoice) uses Invoice in Domain ({Billing}/Domain): no rule allows it (DefaultPermission = Deny). "
            + "Remove the use, or change the rule in Rules."));
    }

    [Test]
    public void A_use_that_a_rule_denies_names_the_rule() {
        Assert.That(UseFailures, Does.Contain(
            $"{Billing}/Slices/CreateInvoice/CreateInvoiceHandler.cs(6): error VSA2001: CreateInvoiceHandler in Handlers.Requests "
            + $"({Billing}/Slices/CreateInvoice) uses InvoicePresenter in Presenters ({Billing}/Presenters): "
            + "the rule \"Slice denies Presenters InSame<Feature> and Module InSame<Feature>\" denies it."));
    }

    [Test]
    public void An_event_of_another_slice_in_the_same_feature_is_denied() {
        Assert.That(UseFailures, Does.Contain(
            $"error VSA2001: InvoiceAuditHandler in Handlers.Events ({Billing}/Slices/AuditInvoice) uses InvoiceCreated in Contracts.Events "
            + $"({Billing}/Slices/CreateInvoice): no rule allows it"));
    }

    [Test]
    public void A_denied_use_of_an_external_DLL_gives_VSA2001() {
        Assert.That(UseFailures, Does.Contain(
            $"error VSA2001: Invoice in Domain ({Billing}/Domain) uses IEntity in KitLayer (Kit.dll): "
            + "the rule \"Domain denies KitLayer\" denies it."));
    }

    [Test]
    public void An_event_of_another_feature_is_allowed() {
        Assert.That(UseFailures, Does.Not.Contain("InvoiceCreatedHandler in"));
    }

    [Test]
    public void The_bad_app_has_only_the_known_use_failures() {
        Assert.That(UseFailures.Split(Environment.NewLine), Has.Length.EqualTo(4), UseFailures);
    }

    [Test]
    public void A_type_that_is_not_the_type_of_its_part_fails_with_VSA2101() {
        Assert.That(Lines("VSA2101"), Is.EqualTo(
            $"{Billing}/Presenters/Formatting.cs(4): error VSA2101: Formatting is in Presenters ({Billing}/Presenters), "
            + "but it is not IPresenter. Move it, or change its base type."));
    }

    [Test]
    public void A_public_member_that_exposes_a_use_only_part_gives_VSA2003() {
        Assert.That(ExposureFailures, Does.Contain(
            $"{Billing}/Presenters/InvoicePresenter.cs(6): error VSA2003: InvoicePresenter in Presenters ({Billing}/Presenters) "
            + $"exposes Invoice in Domain ({Billing}/Domain) in Shown with Public access, "
            + "but the rule \"Presenters uses Domain(Internal)\" allows Internal at most. "
            + "Make the member less visible, or change the rule in Rules."));
    }

    [TestCase("InvoiceCreated", "Current")]
    [TestCase("InvoiceCreated", "Replay(BadApp.Billing.Slices.CreateInvoice.InvoiceCreated)")]
    [TestCase("InvoiceCreated", "Latest")]
    [TestCase("Invoice", "base type")]
    [TestCase("Invoice", "Equals(BadApp.Billing.Domain.Invoice)")]
    public void A_public_member_that_implements_no_framework_interface_gives_VSA2003(string target, string member) {
        Assert.That(ExposureFailures, Does.Contain($"error VSA2003: BellPresenter in Presenters ({Billing}/Presenters) exposes {target} in")
            .And.Contain($") in {member} with Public access"));
    }

    [TestCase("OnEventAsync")]
    [TestCase("IPresenterEvent")]
    [TestCase("in Last ")]
    public void A_framework_interface_in_the_header_and_its_members_and_an_internal_member_give_no_VSA2003(string text) {
        Assert.That(ExposureFailures, Does.Not.Contain(text));
    }

    [Test]
    public void The_bad_app_has_only_the_known_exposure_failures() {
        Assert.That(ExposureFailures.Split(Environment.NewLine), Has.Length.EqualTo(6), ExposureFailures);
    }

    [Test]
    public void A_type_in_an_undeclared_folder_gives_VSA2102() {
        Assert.That(Lines("VSA2102"), Is.EqualTo(
            $"{Billing}/Utils/Clock.cs(4): error VSA2102: Clock is in a folder that the definition does not have. "
            + "Move it to a part, or add the folder in Define."));
    }

    static string Lines(string? code) =>
        string.Join(Environment.NewLine, AllFindings.Value.Where(f => code is null || f.Code == code).Select(FixtureApps.Line));
}

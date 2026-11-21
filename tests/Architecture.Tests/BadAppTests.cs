namespace EngineCore.Tests;

using ArchUnitNET.Domain;
using Steps.Domain.Shared;

public sealed class BadAppTests {
    const string Billing = "BadApp/Features/Billing";
    const string Notes = "BadApp/Features/Notes";
    const string BillingPart = "Features/Billing";
    const string NotesPart = "Features/Notes";

    static readonly Lazy<IReadOnlyList<Finding>> AllFindings = new(() => FixtureApps.Bad.Findings());

    static string UseFailures => Lines("UMP020101");

    static string ExposureFailures => Lines("UMP020102");

    [Test]
    public void The_bad_app_has_only_the_known_findings() {
        Assert.That(AllFindings.Value, Has.Count.EqualTo(18), Lines(null));
    }

    [Test]
    public void A_finding_gives_the_full_path_of_its_file_and_its_line() {
        var finding = AllFindings.Value.Single(f => f.Code == "UMP020202");

        Assert.That(finding.File, Is.EqualTo(Path.Combine(FixtureApps.Root, "BadApp", "Features", "Billing", "Utils", "Clock.cs")));
        Assert.That((finding.Severity, finding.Line), Is.EqualTo((FindingSeverity.Error, (int?)4)));
    }

    [Test]
    public void A_use_that_no_rule_allows_gives_UMP020101_with_the_file_and_line() {
        Assert.That(UseFailures, Does.Contain(
            $"{Notes}/Slices/ReadInvoice/ReadInvoiceHandler.cs(6): error UMP020101: ReadInvoiceHandler in Handlers.Requests "
            + $"({NotesPart}/Slices/ReadInvoice) uses Invoice in Domain ({BillingPart}/Domain): no rule allows it (DefaultPermission = Deny). "
            + "Remove the use, or change the rule in Rules."));
    }

    [Test]
    public void A_use_that_a_rule_denies_names_the_rule() {
        Assert.That(UseFailures, Does.Contain(
            $"{Billing}/Slices/CreateInvoice/CreateInvoiceHandler.cs(6): error UMP020101: CreateInvoiceHandler in Handlers.Requests "
            + $"({BillingPart}/Slices/CreateInvoice) uses InvoicePresenter in Presenters ({BillingPart}/Presenters): "
            + "the rule \"Slice denies Presenters InSame<Feature> and Module InSame<Feature>\" denies it."));
    }

    [Test]
    public void An_event_of_another_slice_in_the_same_feature_is_denied() {
        Assert.That(UseFailures, Does.Contain(
            $"error UMP020101: InvoiceAuditHandler in Handlers.Events ({BillingPart}/Slices/AuditInvoice) uses InvoiceCreated in Contracts.Events "
            + $"({BillingPart}/Slices/CreateInvoice): no rule allows it"));
    }

    [Test]
    public void A_denied_use_of_an_external_DLL_gives_UMP020101() {
        Assert.That(UseFailures, Does.Contain(
            $"error UMP020101: Invoice in Domain ({BillingPart}/Domain) uses IEntity in KitLayer (Kit.dll): "
            + "the rule \"Domain denies KitLayer\" denies it."));
    }

    [Test]
    public void A_use_inside_compiler_generated_code_gives_UMP020101_on_the_declaring_type() {
        Assert.That(UseFailures, Does.Contain(
            "error UMP020101: ArchiveInvoiceHandler in Handlers.Requests "
            + $"({NotesPart}/Slices/ArchiveInvoice) uses Invoice in Domain ({BillingPart}/Domain): "
            + "no rule allows it (DefaultPermission = Deny)."));
    }

    [TestCase("InAsyncMethod")]
    [TestCase("InLambda")]
    [TestCase("InClosure")]
    [TestCase("InLocalFunction")]
    [TestCase("InIterator")]
    public void The_loader_gives_a_use_inside_compiler_generated_code_to_the_declaring_method(string method) {
        var handler = FixtureApps.Bad.App.Types.Single(t => t.Name == "ArchiveInvoiceHandler");
        var uses = handler.Members.OfType<MethodMember>()
            .Single(m => m.Name.StartsWith(method + "(", StringComparison.Ordinal))
            .MemberDependencies.Select(d => d.Target.Name);

        Assert.That(uses, Does.Contain("Invoice"));
    }

    [Test]
    public void The_loader_gives_no_compiler_generated_type() {
        Assert.That(FixtureApps.Bad.App.Types.Select(t => t.FullName), Has.None.Contains("<"));
    }

    [Test]
    public void A_type_in_a_folder_whose_name_starts_with_bin_is_checked() {
        Assert.That(UseFailures, Does.Contain(
            $"{Billing}/Domain/binders/Money.cs(3): error UMP020101: Money in Domain ({BillingPart}/Domain) "
            + "uses IEntity in KitLayer (Kit.dll): "
            + "the rule \"Domain denies KitLayer\" denies it."));
    }

    [Test]
    public void An_event_of_another_feature_is_allowed() {
        Assert.That(UseFailures, Does.Not.Contain("InvoiceCreatedHandler in"));
    }

    [Test]
    public void The_bad_app_has_only_the_known_use_failures() {
        Assert.That(UseFailures.Split(Environment.NewLine), Has.Length.EqualTo(6), UseFailures);
    }

    [Test]
    public void A_type_that_is_not_the_type_of_its_part_fails_with_UMP020201() {
        Assert.That(Lines("UMP020201"), Is.EqualTo(
            $"{Billing}/Presenters/Formatting.cs(4): error UMP020201: Formatting is in Presenters ({BillingPart}/Presenters), "
            + "but it is not IPresenter. Move it, or change its base type."));
    }

    [Test]
    public void A_public_member_that_exposes_a_use_only_part_gives_UMP020102() {
        Assert.That(ExposureFailures, Does.Contain(
            $"{Billing}/Presenters/InvoicePresenter.cs(6): error UMP020102: InvoicePresenter in Presenters ({BillingPart}/Presenters) "
            + $"exposes Invoice in Domain ({BillingPart}/Domain) in Shown with Public access, "
            + "but the rule \"Presenters uses Domain(Internal)\" allows Internal at most. "
            + "Make the member less visible, or change the rule in Rules."));
    }

    [TestCase("InvoiceCreated", "Current")]
    [TestCase("InvoiceCreated", "Replay(BadApp.Billing.Slices.CreateInvoice.InvoiceCreated)")]
    [TestCase("InvoiceCreated", "Latest")]
    [TestCase("Invoice", "base type")]
    [TestCase("Invoice", "Equals(BadApp.Billing.Domain.Invoice)")]
    public void A_public_member_that_implements_no_framework_interface_gives_UMP020102(string target, string member) {
        Assert.That(ExposureFailures, Does.Contain($"error UMP020102: BellPresenter in Presenters ({BillingPart}/Presenters) exposes {target} in")
            .And.Contain($") in {member} with Public access"));
    }

    [TestCase("HandleAsync")]
    [TestCase("IPresenterHandler")]
    [TestCase("in Last ")]
    public void A_framework_interface_in_the_header_and_its_members_and_an_internal_member_give_no_UMP020102(string text) {
        Assert.That(ExposureFailures, Does.Not.Contain(text));
    }

    [Test]
    public void The_bad_app_has_only_the_known_exposure_failures() {
        Assert.That(ExposureFailures.Split(Environment.NewLine), Has.Length.EqualTo(6), ExposureFailures);
    }

    [Test]
    public void A_type_in_an_undeclared_folder_gives_UMP020202() {
        Assert.That(Lines("UMP020202"), Is.EqualTo(
            $"{Billing}/Utils/Clock.cs(4): error UMP020202: Clock is in a folder that the definition does not have. "
            + "Move it to a part, or add the folder in Define."));
    }

    [Test]
    public void A_type_with_a_name_that_breaks_its_name_rule_gives_UMP020204() {
        Assert.That(Lines("UMP020204"), Is.EqualTo(
            $"{Billing}/Commands/Invoices.cs(5): error UMP020204: Invoices is in Commands ({BillingPart}/Commands), "
            + "but the name of a Commands type is <group>. Rename it to Billing, or move it."));
    }

    [Test]
    public void A_count_counts_a_static_class() {
        Assert.That(Lines("UMP020301"), Does.Contain("Feature Features/Billing has 2 Commands types (Billing, Invoices)"));
    }

    [Test]
    public void A_type_of_a_kind_that_the_rule_does_not_allow_gives_UMP020303() {
        Assert.That(Lines("UMP020303"), Is.EqualTo(
            $"{Billing}/Commands/Invoices.cs(5): error UMP020303: Invoices is in Commands ({BillingPart}/Commands), but it is a Class: "
            + "the rule \"Commands has only StaticClass\" allows only StaticClass. Change the type, or move it to another part."));
    }

    [Test]
    public void An_owned_attribute_on_a_member_of_another_part_gives_UMP020304() {
        Assert.That(Lines("UMP020304"), Is.EqualTo(
            $"{Notes}/NotesModule.cs(5): error UMP020304: NotesModule in Module ({NotesPart}) has [CommandAttribute], "
            + "but the rule \"Commands owns CommandAttribute\" keeps it in Commands. "
            + "Move the code with [CommandAttribute] to Commands, or remove the attribute."));
    }

    [Test]
    public void The_type_facts_give_the_kind_and_the_member_attributes() {
        var billing = FixtureApps.Bad.App.Facts.Single(f => f.FullName == "BadApp.Billing.Commands.Billing");

        Assert.That((billing.Kind, billing.IsAbstract), Is.EqualTo((Umpire.Architecture.TypeKind.StaticClass, false)));
        Assert.That(billing.Attributes, Has.Member("Kit.CommandAttribute"));
    }

    static string Lines(string? code) =>
        string.Join(Environment.NewLine, AllFindings.Value.Where(f => code is null || f.Code == code).Select(FixtureApps.Line));
}

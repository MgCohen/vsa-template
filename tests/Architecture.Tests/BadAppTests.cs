using VSA.Architecture.Engine;
using VSA.Testing.Architecture;

namespace Architecture.Tests;

[Explicit("A test of BadAppTests calls the base tests on the bad app.")]
public sealed class BadApp : ArchitectureTests<BadDefinition> {
    public static AppArchitecture App => Architecture;
}

public sealed class BadAppTests {
    const string Billing = "BadApp/Features/Billing";
    const string Notes = "BadApp/Features/Notes";

    static readonly Lazy<string> UseFailures = new(() => Failures(BadApp.App.PartInstances, new BadApp().Uses_only_allowed_parts));

    static readonly Lazy<string> ExposureFailures =
        new(() => Failures(BadApp.App.PartInstances.Where(p => p.Limits.Count > 0), new BadApp().Exposes_only_allowed_parts));

    [Test]
    public void A_use_that_no_rule_allows_gives_VSA2001_with_the_file_and_line() {
        Assert.That(UseFailures.Value, Does.Contain(
            $"{Notes}/Slices/ReadInvoice/ReadInvoiceHandler.cs(6): error VSA2001: ReadInvoiceHandler in Handlers.Requests "
            + $"({Notes}/Slices/ReadInvoice) uses Invoice in Domain ({Billing}/Domain): no rule allows it (Unlisted = Deny). "
            + "Remove the use, or change the rule in Rules."));
    }

    [Test]
    public void A_use_that_a_rule_denies_names_the_rule() {
        Assert.That(UseFailures.Value, Does.Contain(
            $"{Billing}/Slices/CreateInvoice/CreateInvoiceHandler.cs(6): error VSA2001: CreateInvoiceHandler in Handlers.Requests "
            + $"({Billing}/Slices/CreateInvoice) uses InvoicePresenter in Presenters ({Billing}/Presenters): "
            + "the rule \"Slice denies Presenters InOwn<Feature> and RootClass InOwn<Feature>\" denies it."));
    }

    [Test]
    public void An_event_of_another_slice_in_the_same_feature_is_denied() {
        Assert.That(UseFailures.Value, Does.Contain(
            $"error VSA2001: InvoiceAuditHandler in Handlers.Events ({Billing}/Slices/AuditInvoice) uses InvoiceCreated in Contracts.Events "
            + $"({Billing}/Slices/CreateInvoice): no rule allows it"));
    }

    [Test]
    public void A_denied_use_of_an_external_DLL_gives_VSA2001() {
        Assert.That(UseFailures.Value, Does.Contain(
            $"error VSA2001: Invoice in Domain ({Billing}/Domain) uses IEntity in KitLayer (Kit.dll): "
            + "the rule \"Domain denies KitLayer\" denies it."));
    }

    [Test]
    public void An_event_of_another_feature_is_allowed() {
        Assert.That(UseFailures.Value, Does.Not.Contain("InvoiceCreatedHandler in"));
    }

    [Test]
    public void The_bad_app_has_only_the_known_use_failures() {
        Assert.That(UseFailures.Value.Split(Environment.NewLine).Count(line => line.Contains("error VSA2001")), Is.EqualTo(4), UseFailures.Value);
    }

    [Test]
    public void A_type_that_is_not_the_type_of_its_part_fails_with_VSA2101() {
        var failures = Failures(BadApp.App.PartInstances.Where(p => p.OfTypes.Count > 0 && p.Limited.Count > 0), new BadApp().Has_the_type_of_its_part);

        Assert.That(failures, Is.EqualTo(
            $"{Billing}/Presenters/Formatting.cs(4): error VSA2101: Formatting is in Presenters ({Billing}/Presenters), "
            + "but it is not IPresenter. Move it, or change its base type."));
    }

    [Test]
    public void A_public_member_that_exposes_a_use_only_part_gives_VSA2003() {
        Assert.That(ExposureFailures.Value, Does.Contain(
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
        Assert.That(ExposureFailures.Value, Does.Contain($"error VSA2003: BellPresenter in Presenters ({Billing}/Presenters) exposes {target} in")
            .And.Contain($") in {member} with Public access"));
    }

    [TestCase("OnEventAsync")]
    [TestCase("IPresenterEvent")]
    [TestCase("in Last ")]
    public void A_framework_interface_in_the_header_and_its_members_and_an_internal_member_give_no_VSA2003(string text) {
        Assert.That(ExposureFailures.Value, Does.Not.Contain(text));
    }

    [Test]
    public void The_bad_app_has_only_the_known_exposure_failures() {
        Assert.That(ExposureFailures.Value.Split(Environment.NewLine), Has.Length.EqualTo(6), ExposureFailures.Value);
    }

    [Test]
    public void A_type_in_an_undeclared_folder_fails_Each_type_is_in_a_part_with_VSA2102() {
        var failure = Assert.Throws<ArchitectureException>(new BadApp().Each_type_is_in_a_part)!.Message;

        Assert.That(failure, Is.EqualTo(
            $"{Billing}/Utils/Clock.cs(4): error VSA2102: Clock is in a folder that the definition does not have. "
            + "Move it to a part, or add the folder in Define."));
    }

    static string Failures(IEnumerable<PartInstance> instances, Action<PartInstance> test) {
        var failures = new List<string>();
        foreach (var instance in instances) {
            try {
                test(instance);
            } catch (ArchitectureException e) {
                failures.Add(e.Message);
            }
        }
        return string.Join(Environment.NewLine, failures);
    }
}

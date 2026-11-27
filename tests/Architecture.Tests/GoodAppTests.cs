namespace EngineCore.Tests;

using Kit;
using Umpire.CheckModel;

public sealed class GoodAppTests {
    static CheckedApp Map => FixtureApps.Good;

    [Test]
    public void The_good_app_has_no_findings() {
        Assert.That(Map.Findings().Select(FixtureApps.Line), Is.Empty);
    }

    [Test]
    public void Each_type_of_the_good_app_is_in_a_part() {
        Assert.That(Map.Unmapped.Select(u => u.Name.FullName), Is.Empty);
    }

    [TestCase("CreateInvoiceHandler", "Handlers.Requests", "Features/Billing/Slices/CreateInvoice")]
    [TestCase("CreateInvoiceRequest", "Contracts.Requests", "Features/Billing/Slices/CreateInvoice")]
    [TestCase("InvoiceCreated", "Contracts.Events", "Features/Billing/Slices/CreateInvoice")]
    [TestCase("BillingModule", "Feature", "Features/Billing")]
    [TestCase("Invoice", "Domain", "Features/Billing/Domain")]
    public void Each_type_binds_to_its_part_and_place(string type, string part, string place) {
        var placement = Map.Model.PlacementOf(Map.Named(type))!;

        Assert.That(placement.Part.Name, Is.EqualTo(part));
        Assert.That(placement.Path, Is.EqualTo(place));
    }

    [Test]
    public void The_types_of_the_other_app_are_not_bound() {
        Assert.That(Map.Types.Select(t => t.Name.FullName), Has.None.StartsWith("BadApp."));
    }

    [Test]
    public void A_type_of_an_external_DLL_binds_by_the_DLL_name() {
        var placement = External();

        Assert.That(placement.IsExternal, Is.True);
        Assert.That(placement.Part.Name, Is.EqualTo("KitLayer"));
        Assert.That(placement.Where, Is.EqualTo("KitLayer (Kit.dll)"));
    }

    [Test]
    public void A_type_of_an_external_DLL_is_a_rule_target() {
        var domain = Map.Model.PlacementOf(Map.Named("Invoice"))!;
        var handler = Map.Model.PlacementOf(Map.Named("CreateInvoiceHandler"))!;
        var kit = External();

        Assert.That(Map.Model.DecisionFor(domain, kit).Permission, Is.EqualTo(Permission.Deny));
        Assert.That(Map.Model.DecisionFor(handler, kit).Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_type_of_an_external_DLL_is_not_an_app_type() {
        Assert.That(Map.Types.Select(t => t.Name.FullName), Has.None.EqualTo(typeof(IEntity).FullName));
    }

    [Test]
    public void A_member_that_implements_a_framework_interface_exposes_nothing() {
        Assert.That(ExposuresOf("BellPresenter").Select(e => e.Member), Has.None.Contains("HandleAsync"));
    }

    [Test]
    public void A_framework_interface_in_the_header_exposes_nothing() {
        var baseTypes = ExposuresOf("BellPresenter").Where(e => e.Member == "base type").Select(e => e.Target.Name);

        Assert.That(baseTypes, Is.Empty);
    }

    [Test]
    public void A_presenter_with_a_framework_interface_in_the_header_has_no_findings() {
        var file = Map.Named("BellPresenter").File;

        Assert.That(Map.Findings().Where(f => f.File == file), Is.Empty);
    }

    static Placement External() => Map.PlaceOf(typeof(IEntity).FullName!)!;

    static IEnumerable<Exposure> ExposuresOf(string name) => Exposure.Of(Map.Model, Map.Named(name));
}

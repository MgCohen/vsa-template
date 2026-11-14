using Kit;
using VSA.Architecture;
using VSA.Architecture.Engine;

namespace Architecture.Tests;

public sealed class GoodAppTests {
    static ArchitectureMap Map => FixtureApps.Good;

    [Test]
    public void The_good_app_has_no_findings() {
        Assert.That(Map.Findings().Select(FixtureApps.Line), Is.Empty);
    }

    [Test]
    public void Each_type_of_the_good_app_is_in_a_part() {
        Assert.That(Map.Bindings.Unmapped.Select(u => u.Type.FullName), Is.Empty);
    }

    [TestCase("CreateInvoiceHandler", "Handlers.Requests", "GoodApp/Features/Billing/Slices/CreateInvoice")]
    [TestCase("CreateInvoiceRequest", "Contracts.Requests", "GoodApp/Features/Billing/Slices/CreateInvoice")]
    [TestCase("InvoiceCreated", "Contracts.Events", "GoodApp/Features/Billing/Slices/CreateInvoice")]
    [TestCase("BillingModule", "Module", "GoodApp/Features/Billing")]
    [TestCase("Invoice", "Domain", "GoodApp/Features/Billing/Domain")]
    public void Each_type_binds_to_its_part_and_place(string type, string part, string place) {
        var binding = Map.Bindings.All.Single(b => b.Type.Name == type && !b.External);

        Assert.That(binding.Part.Name, Is.EqualTo(part));
        Assert.That(binding.Path, Is.EqualTo(place));
    }

    [Test]
    public void The_types_of_the_other_app_are_not_bound() {
        Assert.That(Map.Bindings.AppTypes.Select(t => t.FullName), Has.None.StartsWith("BadApp."));
    }

    [Test]
    public void A_type_of_an_external_DLL_binds_by_the_DLL_name() {
        var binding = External();

        Assert.That(binding.External, Is.True);
        Assert.That(binding.Part.Name, Is.EqualTo("KitLayer"));
        Assert.That(binding.Where, Is.EqualTo("KitLayer (Kit.dll)"));
    }

    [Test]
    public void A_type_of_an_external_DLL_is_a_rule_target() {
        var domain = Map.Bindings.All.Single(b => b.Type.Name == "Invoice" && !b.External);
        var handler = Map.Bindings.All.Single(b => b.Type.Name == "CreateInvoiceHandler");

        Assert.That(Decide.For(Map.Definition, domain, External()).Permission, Is.EqualTo(Permission.Deny));
        Assert.That(Decide.For(Map.Definition, handler, External()).Permission, Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_type_of_an_external_DLL_is_not_an_app_type() {
        Assert.That(Map.Bindings.AppTypes.Select(t => t.FullName), Has.None.EqualTo(typeof(IEntity).FullName));
    }

    [Test]
    public void A_member_that_implements_a_framework_interface_exposes_nothing() {
        Assert.That(ExposuresOf("BellPresenter").Select(e => e.Member), Has.None.Contains("OnEventAsync"));
    }

    [Test]
    public void A_framework_interface_in_the_header_exposes_nothing() {
        var baseTypes = ExposuresOf("BellPresenter").Where(e => e.Member == "base type").Select(e => e.Target.Name);

        Assert.That(baseTypes, Is.EqualTo(new[] { "Object" }));
    }

    [Test]
    public void A_presenter_with_a_framework_interface_in_the_header_has_no_findings() {
        var presenter = Map.Bindings.BoundTypes.Single(t => t.Name == "BellPresenter");

        Assert.That(Map.Findings(presenter), Is.Empty);
    }

    static Binding External() => Map.Bindings.All.Single(b => b.Type.FullName == typeof(IEntity).FullName);

    static IEnumerable<Exposure> ExposuresOf(string name) =>
        Exposures.Of(Map.Bindings.BoundTypes.Single(t => t.Name == name), Map.Bindings.Framework.Contains);
}

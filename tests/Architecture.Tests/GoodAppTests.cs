using Kit;
using VSA.Testing.Architecture;

namespace Architecture.Tests;

public sealed class GoodAppTests : ArchitectureTests<GoodDefinition> {
    [TestCase("CreateInvoiceHandler", "Handlers.Requests", "GoodApp/Features/Billing/Slices/CreateInvoice")]
    [TestCase("CreateInvoiceRequest", "Contracts.Requests", "GoodApp/Features/Billing/Slices/CreateInvoice")]
    [TestCase("InvoiceCreated", "Contracts.Events", "GoodApp/Features/Billing/Slices/CreateInvoice")]
    [TestCase("BillingModule", "RootClass", "GoodApp/Features/Billing")]
    [TestCase("Invoice", "Domain", "GoodApp/Features/Billing/Domain")]
    public void Each_type_binds_to_its_part_and_place(string type, string part, string place) {
        var binding = Architecture.Bindings.Single(b => b.Type.Name == type && !b.External);

        Assert.That((binding.Part.Name, binding.Instance), Is.EqualTo((part, place)));
    }

    [Test]
    public void The_types_of_the_other_app_are_not_bound() {
        Assert.That(Architecture.AppTypes.Select(t => t.FullName), Has.None.StartsWith("BadApp."));
    }

    [Test]
    public void A_type_of_an_external_DLL_binds_by_the_DLL_name() {
        var binding = Architecture.Bindings.Single(b => b.Type.FullName == typeof(IEntity).FullName);

        Assert.That((binding.External, binding.Part.Name, binding.Where), Is.EqualTo((true, "KitLayer", "KitLayer (Kit.dll)")));
    }

    [Test]
    public void A_type_of_an_external_DLL_is_a_rule_target() {
        var domain = Architecture.PartInstances.Single(p => p.Name == "Domain (GoodApp/Features/Billing/Domain)");
        var handler = Architecture.PartInstances.Single(p => p.Name == "Handlers.Requests (GoodApp/Features/Billing/Slices/CreateInvoice)");

        Assert.That(domain.Denied.Select(t => t.FullName), Does.Contain(typeof(IEntity).FullName));
        Assert.That(handler.Denied.Select(t => t.FullName), Does.Not.Contain(typeof(IEntity).FullName));
    }

    [Test]
    public void An_external_DLL_is_not_a_part_instance() {
        Assert.That(Architecture.PartInstances.Select(p => p.Name), Has.None.StartsWith("KitLayer"));
    }
}

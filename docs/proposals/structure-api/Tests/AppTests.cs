using ArchUnitNET.NUnit;
using Structure;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Tests;

static class App
{
    public static string Dll(string name) => Path.Combine(AppContext.BaseDirectory, "apps", name + ".dll");
}

public class GoodAppTests() : StructureTests<VsaStructure>(App.Dll("GoodApp"))
{
    [Test]
    public void X01_handlers_do_not_use_the_host() =>
        Types().That().Are(Def.Handlers).Should().NotDependOnAny(Def.Host).Check(Arch);

    [Test]
    public void X02_domain_of_billing_is_not_empty() =>
        Assert.That(Def.Domain.In("Billing").GetObjects(Arch), Is.Not.Empty);
}

[Explicit("BadApp has 7 planted errors. Run it to see the failure messages.")]
public class BadAppTests() : StructureTests<VsaStructure>(App.Dll("BadApp"));

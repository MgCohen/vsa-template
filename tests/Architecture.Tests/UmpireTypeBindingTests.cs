namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class UmpireTypeBindingTests {
    const string AppFolder = "C:/app";
    static readonly ResolvedArchitecture Resolved = new(new VsaArchitecture());

    [Test]
    public void An_Umpire_type_outside_the_kernel_bodies_binds_to_the_Umpire_libraries() {
        var config = Bind(Fact("Umpire.Config.UmpireConfig", "Umpire.Config", "C:/repo/framework/Components/Config/UmpireConfig.cs"));

        Assert.That(config?.Part.Name, Is.EqualTo(nameof(UmpireLibraries)));
    }

    [Test]
    public void A_domain_type_cannot_use_an_Umpire_type_outside_the_kernel_bodies() {
        var domain = Bind(Fact("App.Features.Notes.Domain.Note", "App", $"{AppFolder}/Features/Notes/Domain/Note.cs"))!;
        var config = Bind(Fact("Umpire.Config.UmpireConfig", "Umpire.Config", "C:/repo/framework/Components/Config/UmpireConfig.cs"))!;

        var decision = Resolved.DecisionFor(domain.Part, domain.Values, config.Part, config.Values);

        Assert.That(decision.Permission, Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void An_Umpire_type_in_a_kernel_body_binds_to_its_layer() {
        var sender = Bind(Fact(
            "Umpire.Vsa.Contracts.Requests.IRequestSender", "Umpire.Vsa.Contracts.Requests",
            "C:/repo/packs/Vsa/Kernel/Contracts/Requests/IRequestSender.cs"));

        Assert.That(sender?.Part.SelfAndAncestors().Select(p => p.Name), Is.EqualTo(new[] { "Contracts.Requests", "Contracts", "VsaKernel", "(root)" }));
    }

    [Test]
    public void A_type_of_another_library_with_kernel_folder_names_has_no_part() {
        var shared = Bind(Fact("Shared.Messages.Contracts.OrderPlaced", "Shared", "C:/work/Shared/Messages/Contracts/OrderPlaced.cs"));

        Assert.That(shared, Is.Null);
    }

    static Binding? Bind(TypeFacts type) => Resolved.Place([type], AppFolder).Of(type.FullName);

    static TypeFacts Fact(string fullName, string assembly, string file) =>
        new(fullName, fullName[(fullName.LastIndexOf('.') + 1)..], assembly, new Source(file, 1), [], [], [], []);
}

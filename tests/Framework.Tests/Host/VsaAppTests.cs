using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Architecture;
using VSA.Architecture.Vsa;
using Microsoft.Extensions.Configuration;
using Vsa = VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Host;

namespace Framework.Tests.Host;

public sealed class VsaAppTests {
    [Test]
    public void Run_makes_the_app_and_runs_its_start_steps() {
        var exitCode = StartedApp.Run(["--name", "test"]);

        Assert.That((exitCode, StartedApp.Marker), Is.EqualTo((7, "test")));
    }

    [Test]
    public void An_app_with_no_overrides_has_the_VSA_rules() {
        Assert.That(Rules(new PlainApp().Architecture), Is.EqualTo(Rules(new VsaDefinition())));
    }

    [Test]
    public void A_Rules_override_adds_its_rules_after_the_VSA_rules() {
        var app = Rules(new RulesApp().Architecture);
        var vsa = Rules(new VsaDefinition());

        Assert.That(app.Take(vsa.Count), Is.EqualTo(vsa));
        Assert.That(app[^1], Is.EqualTo(("Presenters", "Contracts.Events", Permission.Deny)));
    }

    [TestCase("ArchUnitNET")]
    [TestCase("Mono.Cecil")]
    [TestCase("nunit.framework")]
    public void The_runtime_of_an_app_does_not_reference_the_test_tools(string tool) {
        Assert.That(RuntimeReferences(typeof(VsaApp<>).Assembly), Has.None.StartsWith(tool));
    }

    static List<(string From, string To, Permission Permission)> Rules(ArchitectureDefinition definition) =>
        definition.Resolve().Select(p => (p.From.Name, p.To.Name, p.Permission)).ToList();

    static HashSet<string> RuntimeReferences(Assembly start) {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var next = new Queue<AssemblyName>([start.GetName()]);
        while (next.TryDequeue(out var name)) {
            if (!found.Add(name.Name!)) {
                continue;
            }
            foreach (var reference in Assembly.Load(name).GetReferencedAssemblies()) {
                next.Enqueue(reference);
            }
        }
        return found;
    }

    sealed class PlainApp : VsaApp<PlainApp>;

    sealed class RulesApp : VsaApp<RulesApp> {
        protected override void Rules(RuleBook rules) => rules.For<Vsa.Presenters>().Denies<Vsa.Contracts.Events>();
    }

    sealed class StartedApp : VsaApp<StartedApp> {
        public static string? Marker { get; private set; }

        protected override int Start(string[] args) {
            using var host = Build(args);
            Marker = host.Services.GetRequiredService<IConfiguration>()["name"];
            return host.Services.GetRequiredService<ExitCode>().Value;
        }

        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).UseGenericHost().Modules();

        protected override void Services(IServiceCollection services) => services.AddSingleton(new ExitCode(7));
    }

    sealed record ExitCode(int Value);
}

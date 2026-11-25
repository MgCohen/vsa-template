namespace Rules.Tests;

using Umpire.Vsa.Analyzers;

public sealed class PackSelectionTests {
    const string Request = """
        public sealed class {|#0:GetItems|} : Umpire.Vsa.Contracts.Requests.IRequest<Items>;
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """;
    const string QuietRequest = """
        public sealed class GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items>;
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """;
    const string OtherKernel = """
        public sealed class OtherKernel : Umpire.Kernels.Kernel {
            protected override void Configure(
                Microsoft.Extensions.DependencyInjection.IServiceCollection services, Umpire.Kernels.AppTypes types) { }
        }
        """;
    const string OtherArchitecture = "umpire.architecture = Other.OtherArchitecture, Other\n";

    [Test]
    public Task A_compilation_with_no_entry_that_references_the_pack_runs_its_rules() =>
        Analyzer.Verify(Request, Found());

    [Test]
    public Task An_app_class_of_the_pack_runs_its_rules() => Analyzer.Verify(Request + """
        public sealed class ShopApp : Umpire.Kernels.App<Umpire.Vsa.Composition.VsaKernel, Umpire.Kernels.AppHost>;
        """, Found());

    [Test]
    public Task A_derived_kernel_runs_the_rules_of_its_base_pack() => Analyzer.Verify(Request + """
        public sealed class EcsKernel : Umpire.Vsa.Composition.VsaKernel;
        public sealed class ShopApp : Umpire.Kernels.App<EcsKernel, Umpire.Kernels.AppHost>;
        """, Found());

    [Test]
    public Task An_app_class_of_another_pack_runs_no_rule_of_this_pack() => Analyzer.Verify(QuietRequest + OtherKernel + """
        public sealed class ShopApp : Umpire.Kernels.App<OtherKernel, Umpire.Kernels.AppHost>;
        """);

    [Test]
    public Task A_block_of_a_pack_architecture_runs_its_rules() => Analyzer.Verify(Request + """
        public sealed class ToolsBlock : Umpire.Architecture.Block<Umpire.Vsa.Definition.VsaArchitecture>;
        """, Found());

    [Test]
    public Task A_definition_of_a_kernel_of_the_pack_runs_its_rules() => Analyzer.Verify(Request + """
        public sealed class ShopArchitecture : Umpire.Vsa.Definition.VsaArchitecture;
        """, Found());

    [Test]
    public Task A_definition_of_another_kernel_runs_no_rule_of_this_pack() => Analyzer.Verify(QuietRequest + OtherKernel + """
        public sealed class ShopArchitecture : Umpire.Kernels.Architecture<OtherKernel> {
            protected override void Define(Umpire.Architecture.Part root, Umpire.Architecture.Part kernel) { }
        }
        """);

    [Test]
    public Task A_project_with_only_a_kernel_of_the_pack_runs_its_rules() => Analyzer.Verify(Request + """
        public sealed class EcsKernel : Umpire.Vsa.Composition.VsaKernel;
        """, Found());

    [Test]
    public Task A_project_with_only_a_kernel_of_another_pack_runs_no_rule_of_this_pack() =>
        Analyzer.Verify(QuietRequest + OtherKernel);

    [Test]
    public Task Assembly_metadata_that_names_the_pack_architecture_runs_its_rules() => Analyzer.Verify("""
        [assembly: System.Reflection.AssemblyMetadata("Umpire.Architecture", "Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa")]
        """ + Request, Found());

    [Test]
    public Task Assembly_metadata_that_names_another_architecture_runs_no_rule_of_this_pack() => Analyzer.Verify("""
        [assembly: System.Reflection.AssemblyMetadata("Umpire.Architecture", "Other.OtherArchitecture, Other")]
        """ + QuietRequest);

    [Test]
    public Task The_umpireconfig_key_names_the_architecture_of_a_block_with_no_code_entry() =>
        Analyzer.Verify(QuietRequest, OtherArchitecture);

    [Test]
    public Task An_entry_in_code_wins_over_the_umpireconfig_key() => Analyzer.Verify(Request + """
        public sealed class ShopApp : Umpire.Kernels.App<Umpire.Vsa.Composition.VsaKernel, Umpire.Kernels.AppHost>;
        """, OtherArchitecture, Found());

    [Test]
    public Task A_nested_app_class_of_another_pack_runs_no_rule_of_this_pack() => Analyzer.Verify(QuietRequest + OtherKernel + """
        public static class Hosts {
            public sealed class ShopApp : Umpire.Kernels.App<OtherKernel, Umpire.Kernels.AppHost>;
        }
        """);

    [Test]
    public Task An_open_generic_app_base_is_no_entry() => Analyzer.Verify(Request + """
        public abstract class GameApp<TKernel> : Umpire.Kernels.App<TKernel, Umpire.Kernels.AppHost> where TKernel : Umpire.Kernels.Kernel, new();
        """, Found());

    [Test]
    public Task An_app_class_on_an_open_generic_base_selects_the_pack_of_its_argument() =>
        Analyzer.Verify(QuietRequest + OtherKernel + """
        public abstract class GameApp<TKernel> : Umpire.Kernels.App<TKernel, Umpire.Kernels.AppHost> where TKernel : Umpire.Kernels.Kernel, new();
        public sealed class ShopApp : GameApp<OtherKernel>;
        """);

    [Test]
    public Task A_launcher_that_implements_the_entry_interface_of_another_pack_runs_no_rule_of_this_pack() =>
        Analyzer.Verify(QuietRequest + OtherKernel + """
        public abstract class Launcher<TKernel> : Umpire.Kernels.IAppEntry<TKernel> where TKernel : Umpire.Kernels.Kernel, new();
        public sealed class GameLauncher : Launcher<OtherKernel>;
        """);

    [Test]
    public Task A_launcher_that_implements_the_entry_interface_of_the_pack_runs_its_rules() => Analyzer.Verify(Request + """
        public sealed class GameLauncher : Umpire.Kernels.IAppEntry<Umpire.Vsa.Composition.VsaKernel>;
        """, Found());

    [Test]
    public Task An_abstract_class_that_implements_the_entry_interface_is_no_entry() =>
        Analyzer.Verify(QuietRequest + OtherKernel + """
        public abstract class Launcher : Umpire.Kernels.IAppEntry<Umpire.Vsa.Composition.VsaKernel>;
        """);

    [Test]
    public Task A_class_that_implements_the_entry_interface_for_two_packs_gives_an_entry_for_each() =>
        Analyzer.Verify(Request + OtherKernel + """
        public sealed class GameLauncher : Umpire.Kernels.IAppEntry<OtherKernel>, Umpire.Kernels.IAppEntry<Umpire.Vsa.Composition.VsaKernel>;
        """, Found());

    static Microsoft.CodeAnalysis.Testing.DiagnosticResult Found() =>
        Analyzer.Diagnostic(new RecordPayloadRule().Rule, 0, "GetItems", "a request");
}

namespace Rules.Tests;

using Umpire.Vsa.Analyzers;

public sealed class PackSelectionTests {
    const string Route = "public static HttpMethod Method => HttpMethod.Get; public static string Route => \"/items\";";
    const string Request = $$"""
        public sealed class {|#0:GetItems|} : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """;
    const string QuietRequest = $$"""
        public sealed class GetItems : Umpire.Vsa.Contracts.Requests.IRequest<Items> { {{Route}} }
        public sealed record Items(int Count) : Umpire.Vsa.Contracts.Requests.IResponse;
        """;
    const string OtherPack = """
        public sealed class OtherPack : Umpire.Kernel.Pack {
            public override Umpire.Architecture.Architecture Architecture => null!;
            public override Umpire.Kernel.AppHost CreateHost() => null!;
        }
        """;
    const string OtherArchitecture = "umpire.architecture = Other.OtherArchitecture, Other\n";

    [Test]
    public Task A_compilation_with_no_entry_that_references_the_pack_runs_its_rules() =>
        Analyzer.Verify(Request, Found());

    [Test]
    public Task An_app_class_of_the_pack_runs_its_rules() => Analyzer.Verify(Request + """
        public sealed class ShopApp : Umpire.Kernel.App<Umpire.Vsa.Vsa<Umpire.Vsa.Composition.ConsoleVsaHost>>;
        """, Found());

    [Test]
    public Task A_pack_variant_runs_the_rules_of_its_base_pack() => Analyzer.Verify(Request + """
        public sealed class EcsPack : Umpire.Vsa.Vsa<Umpire.Vsa.Composition.ConsoleVsaHost>;
        public sealed class ShopApp : Umpire.Kernel.App<EcsPack>;
        """, Found());

    [Test]
    public Task An_app_class_of_another_pack_runs_no_rule_of_this_pack() => Analyzer.Verify(QuietRequest + OtherPack + """
        public sealed class ShopApp : Umpire.Kernel.App<OtherPack>;
        """);

    [Test]
    public Task A_block_of_a_pack_architecture_runs_its_rules() => Analyzer.Verify(Request + """
        public sealed class ToolsBlock : Umpire.Architecture.Block<Umpire.Vsa.Definition.VsaArchitecture>;
        """, Found());

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
        public sealed class ShopApp : Umpire.Kernel.App<Umpire.Vsa.Vsa<Umpire.Vsa.Composition.ConsoleVsaHost>>;
        """, OtherArchitecture, Found());

    static Microsoft.CodeAnalysis.Testing.DiagnosticResult Found() =>
        Analyzer.Diagnostic(SealedRecordRule.Descriptor, 0, "GetItems", "a request");
}

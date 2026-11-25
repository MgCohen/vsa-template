namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class ReferenceTrustTests {
    const string Store = "namespace Sample;\npublic class Store : Base;\n";

    [Test]
    public void A_project_that_did_not_compile_against_the_saved_reference_gives_a_warning() {
        using var build = Built();
        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));

        build.Write("Ui/Base.cs", "namespace Sample;\npublic interface IStore;\npublic class Base : IStore;\n");
        build.Analyze(build.Compile("Ui", "Ui/Base.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_changed_method_body_of_the_reference_does_not_lower_the_finding() {
        using var build = Built();

        build.Write("Ui/Base.cs", "namespace Sample;\n\npublic class Base { public int Run() { return 2; } }\n");
        build.Analyze(build.Compile("Ui", "Ui/Base.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    [TestCase("private int Hidden() => 2;")]
    [TestCase("internal int Hidden() => 2;")]
    public void A_member_that_a_dependent_project_does_not_see_does_not_lower_the_finding(string member) {
        using var build = Built();

        build.Write("Ui/Base.cs", $"namespace Sample;\npublic class Base {{ public int Run() {{ return 1; }} {member} }}\n");
        build.Analyze(build.Compile("Ui", "Ui/Base.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    [TestCase("[System.Obsolete] private int Hidden() => 2;")]
    [TestCase("private int Field;")]
    public void A_private_member_with_an_attribute_does_not_change_the_surface(string member) {
        using var build = new SampleBuild("Data");
        var first = SurfaceOf(build, "public partial class Base { public int P { get; private set; } }");

        Assert.That(SurfaceOf(build, $"public partial class Base {{ public int P {{ get; }} {member} }}"), Is.EqualTo(first));
        Assert.That(SurfaceOf(build, "public partial class Base { public int P { get; set; } }"), Is.Not.EqualTo(first));
    }

    [Test]
    public void A_public_member_in_a_part_with_no_file_changes_the_surface() {
        using var build = new SampleBuild("Data");
        var first = Surfaces(build, "public partial class Other { public int P => 1; }", "partial ");

        Assert.That(Surfaces(build, "public partial class Other { public string P => null!; }", "partial "), Is.Not.EqualTo(first));
    }

    [Test]
    public void A_new_file_of_a_nested_project_is_not_hidden_by_a_compile_of_the_root() {
        using var build = new SampleBuild("Root", "Data");
        build.Save(Definition, Samples.Settings("Root", "Data"));
        build.Options["build_property.ProjectDir"] = build.Block;
        build.Write("Ui/Base.cs", "namespace Sample;\npublic class Base;\n");
        build.Write("Data/Data.csproj", "");
        build.Write("Data/Store.cs", Store);
        var root = build.Compile("Root", "Ui/Base.cs");
        build.Analyze(root);
        build.Options["build_property.ProjectDir"] = Path.Combine(build.Block, "Data");
        build.Analyze(build.Compile("Data", [root], "Data/Store.cs"));
        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));

        build.Write("Data/Store.Part.cs", "namespace Sample;\npublic partial class Store;\n");
        build.Options["build_property.ProjectDir"] = build.Block;
        build.Analyze(root);

        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_new_file_of_a_nested_project_with_no_snapshot_does_not_lower_a_finding() {
        using var build = new SampleBuild("Ui", "Data");
        build.Options["build_property.ProjectDir"] = build.Block;
        build.Write("Tests/Tests.csproj", "");
        build.Write("Tests/First.cs", "class First;");
        build.Write("Tests/Shared.cs", "namespace Sample;\npublic class Shared;\n");
        Fill(build);
        build.Analyze(build.Compile("Ui", "Ui/Base.cs", "Tests/Shared.cs"));
        build.Analyze(build.Compile("Data", [build.Compile("Ui", "Ui/Base.cs", "Tests/Shared.cs")], "Data/Store.cs"));

        build.Write("Tests/Second.cs", "class Second;");

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    [Test]
    public void A_file_with_an_old_time_that_moves_into_the_project_lowers_the_finding() {
        using var build = new SampleBuild("Ui", "Data");
        build.Write("Data/Store.Part.txt", "namespace Sample;\npublic partial class Store;\n");
        Fill(build);

        File.Move(Path.Combine(build.Block, "Data", "Store.Part.txt"), Path.Combine(build.Block, "Data", "Store.Part.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_changed_type_with_no_file_changes_the_surface() {
        using var build = new SampleBuild("Data");
        var first = Surfaces(build, "public class Base;");

        Assert.That(Surfaces(build, "public interface IStore; public class Base : IStore;"), Is.Not.EqualTo(first));
    }

    [Test]
    public void An_excluded_file_does_not_lower_a_count_in_the_analyzer_of_one_project() {
        using var build = new SampleBuild();
        build.Save(Samples.Layers with {
            Counts = [new CountRule(
                [Samples.Entry(Samples.Data)], [1], false, true, false, false, new CountedTypes(false, [0], Samples.Ui), false, Min: 1)],
        });
        build.Write("Data/Store.cs", "namespace Sample;\npublic class Store;\n");
        build.Write("Data/Excluded.cs", "class Excluded;");

        var diagnostic = build.Analyze(build.Compile("Sample", "Data/Store.cs")).Single();

        Assert.That(diagnostic.Severity, Is.EqualTo(Microsoft.CodeAnalysis.DiagnosticSeverity.Error));
    }

    [Test]
    public void A_new_file_lowers_the_finding_of_a_saved_type() {
        using var build = Built();

        build.Write("Data/Store.Part.cs", "namespace Sample;\npublic partial class Store;\n");

        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void An_older_file_that_no_project_compiles_does_not_lower_a_finding() {
        using var build = new SampleBuild("Ui", "Data");
        build.Write("Data/Excluded.cs", "class Excluded;");

        Fill(build);

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    [Test]
    public void Two_saved_projects_that_reference_each_other_do_not_stop_the_job() {
        using var build = Built();
        var ui = build.Saved("Ui");
        build.State.Save(ui with { References = [.. ui.References, new ProjectReference("Data", "old")] });

        Assert.That(CheckJob.Run(build.State).Succeeded, Is.True);
        Assert.That(WrongType(build), Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_new_config_file_lowers_an_empty_block_place() {
        using var build = new SampleBuild();
        var place = Samples.Part(Samples.Concept("Pack"), "Packs") with { IsGroup = true, IsBlockPlace = true };
        build.Save(Samples.Layers with { Parts = [.. Samples.Layers.Parts, place] });
        Directory.CreateDirectory(Path.Combine(build.Block, "Packs", "New"));

        build.Write("Packs/New/.umpireconfig", "");

        Assert.That(CheckJob.Run(build.State).Findings.Single(f => f.Code == RuleCodes.EmptyBlockPlace).Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_generated_base_with_no_file_does_not_lower_the_finding() {
        using var build = new SampleBuild("Data");
        build.Save(Definition, Samples.Settings("Data"));
        build.Write("Data/Store.cs", Store);
        build.Write("obj/Base.cs", "namespace Sample;\npublic class Base;\n");

        build.Analyze(build.Compile("Data", "Data/Store.cs", "obj/Base.cs"));

        Assert.That(WrongType(build), Is.EqualTo(Severity.Error));
    }

    static Definition Definition => Samples.Layers with {
        Parts = [Samples.Layers.Parts[0], Samples.Layers.Parts[1] with { Types = [Samples.Name("IStore")] }],
        Uses = [new UseRule(false, [Samples.Entry(Samples.Data)], Permission.Allow, [Samples.Entry(Samples.Ui)],
            [new UsePair(1, 0, Access.Public, [], [])])],
    };

    static string SurfaceOf(SampleBuild build, string type) {
        build.Write("Data/Base.cs", $"namespace Sample;\n{type}\n");
        return Surface.Of(new Umpire.CheckModel.Analyzer.ProjectFacts(build.Compile("Data", "Data/Base.cs"), build.Block).Declared);
    }

    static string Surfaces(SampleBuild build, string generated, string modifier = "") {
        build.Write("obj/Base.cs", $"namespace Sample;\n{generated}\n");
        build.Write("Data/Other.cs", $"namespace Sample;\npublic {modifier}class Other;\n");
        return Surface.Of(new Umpire.CheckModel.Analyzer.ProjectFacts(build.Compile("Data", "Data/Other.cs", "obj/Base.cs"), build.Block).Declared);
    }

    static SampleBuild Built() {
        var build = new SampleBuild("Ui", "Data");
        Fill(build);
        return build;
    }

    static void Fill(SampleBuild build) {
        build.Save(Definition);
        build.Write("Ui/Base.cs", "namespace Sample;\npublic class Base { public int Run() { return 1; } }\n");
        build.Write("Data/Store.cs", Store);
        var ui = build.Compile("Ui", "Ui/Base.cs");
        build.Analyze(ui);
        build.Analyze(build.Compile("Data", [ui], "Data/Store.cs"));
    }

    static Severity WrongType(SampleBuild build) =>
        CheckJob.Run(build.State).Findings.Single(f => f.Code == RuleCodes.WrongType).Severity;
}

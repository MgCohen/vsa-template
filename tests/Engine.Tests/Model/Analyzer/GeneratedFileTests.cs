namespace CheckModel.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Umpire.CheckModel;
using Umpire.CheckModel.Analyzer;

public sealed class GeneratedFileTests {
    [TestCase("Generator/Assembly/Hint.g.cs")]
    [TestCase("obj/Debug/generated/Assembly/Generator/Hint.g.cs")]
    [TestCase("{block}/obj/Debug/generated/Assembly/Generator/Hint.g.cs")]
    [TestCase("{block}/obj/Debug/generated/Assembly/Generator/generated/Hint.g.cs")]
    public void A_partial_type_is_in_the_folder_of_its_written_file(string generated) {
        using var build = new SampleBuild();
        build.Write("Data/Store.cs", "namespace Sample;\n\n\n\n\npublic partial class Store;\n");
        var path = generated.Replace("{block}", build.Block);
        var compilation = build.Compile("Sample", "Data/Store.cs")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("namespace Sample;\npartial class Store;\n", path: path));
        var facts = new ProjectFacts(compilation, build.Block, build.Block);
        build.State.Save(facts.Full());

        var type = AppModel.Load(build.State).Types.Single();

        Assert.That(type.Files, Has.Count.EqualTo(2));
        Assert.That(BlockPath.Of(build.Block, type.File), Is.EqualTo("Data/Store.cs"));
        Assert.That(type.Files[1].File, Does.EndWith("Hint.g.cs").And.StartsWith(build.Block));
    }
}

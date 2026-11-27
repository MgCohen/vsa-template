namespace CheckModel.Tests;

using Umpire.CheckModel;
using UmpireEngine;

public sealed class AttributeTests {
    [TestCase("class S { int P { [Mark] get; } }")]
    [TestCase("class S { int P { get; [Mark] set; } }")]
    [TestCase("class S { [field: Mark] int P { get; } }")]
    [TestCase("class S { [return: Mark] int M() => 0; }")]
    [TestCase("class S { void M([Mark] int p) { } }")]
    [TestCase("class S { int this[[Mark] int p] => 0; }")]
    [TestCase("class S { void M<[Mark] T>() { } }")]
    [TestCase("class S<[Mark] T>;")]
    [TestCase("class S { event System.Action E { [Mark] add { } remove { } } }")]
    [TestCase("record S([property: Mark] int P);")]
    [TestCase("record S([field: Mark] int P);")]
    [TestCase("class S([Mark] int p) { int P => p; }")]
    [TestCase("delegate void S([Mark] int p);")]
    public void The_facts_have_the_attribute(string type) {
        using var build = new SampleBuild();
        build.Write("Ui/S.cs", $"namespace Sample;\npublic class MarkAttribute : System.Attribute;\npublic {type}\n");
        var facts = new ProjectFacts(build.Compile("Sample", "Ui/S.cs"), build.Block).Full();

        var content = facts.Files.Single().Types.Single(t => t.Name.Name.StartsWith('S')).Content;

        Assert.That(content.Attributes.Select(a => a.Type.Name), Has.Member("MarkAttribute"));
    }
}

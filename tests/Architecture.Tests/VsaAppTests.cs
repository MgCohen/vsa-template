namespace EngineCore.Tests;

using Rules.Slices.CheckRules;
using Umpire.Vsa.Definition;

public sealed class VsaAppArchitecture() : VsaArchitecture("VsaApp");

public sealed class VsaAppTests {
    const string Notebook = "VsaApp/Features/Notebook/";

    static readonly Lazy<ArchitectureCheck> VsaMap =
        new(() => FixtureApps.Load(new VsaAppArchitecture(), AppContext.BaseDirectory, FixtureApps.Root));

    static readonly string[] Known = [
        Notebook + "Contracts/StrayContract.cs(3): error UMP020202",
        Notebook + "Slices/Archive/ArchiveAuditHook.cs(7): error UMP020301",
        Notebook + "Slices/Count/CountRequest.cs(5): error UMP020301",
        Notebook + "Slices/Count/CountResponse.cs(5): error UMP020301",
        Notebook + "Slices/Draft/DraftNote.cs(3): error UMP020301",
        Notebook + "Slices/LooseRecord.cs(3): error UMP020202",
        Notebook + "Slices/Peek/PeekRequest.cs(5): error UMP020101",
        Notebook + "Slices/Ping/PingRequest.cs(5): error UMP020301",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020302",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020302",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020101",
        Notebook + "Slices/Pong/PongHandler.cs(9): error UMP020101",
        "VsaApp/Features/Rogue/RoguePlugin.cs(7): error UMP020101",
        "VsaApp/Features/Twin/TwinExtraModule.cs(7): error UMP020301",
        "VsaApp/Plugins/Stray/StrayModule.cs(7): error UMP020101",
        "VsaApp/Plugins/Stray/StrayModule.cs(7): error UMP020301",
        "VsaApp/Plugins/Tools/Infrastructure/NoteReader.cs(6): error UMP020101",
        "VsaApp/Plugins/Tools/Slices/Run/RunStep.cs(3): error UMP020202",
    ];

    static IReadOnlyList<string> Findings => [.. VsaMap.Value.Findings().Select(FixtureApps.Line)];

    [TestCase(Notebook + "Contracts/StrayContract.cs", "UMP020202", "StrayContract is in a folder")]
    [TestCase("VsaApp/Plugins/Tools/Slices/Run/RunStep.cs", "UMP020202", "RunStep is in a folder")]
    [TestCase(Notebook + "Slices/LooseRecord.cs", "UMP020202", "LooseRecord is in a folder")]
    [TestCase(Notebook + "Slices/Peek/PeekRequest.cs", "UMP020101", "PeekRequest in Contracts.Requests")]
    [TestCase(Notebook + "Slices/Draft/DraftNote.cs", "UMP020301", "Slice VsaApp/Features/Notebook/Slices/Draft has 0 Handlers types")]
    [TestCase(Notebook + "Slices/Archive/ArchiveAuditHook.cs", "UMP020301", "has Handlers types of 2 kinds")]
    [TestCase(Notebook + "Slices/Count/CountRequest.cs", "UMP020301", "has 2 Handlers.Requests types that take it")]
    [TestCase(Notebook + "Slices/Pong/PongHandler.cs", "UMP020302", "takes PingRequest in Contracts.Requests")]
    [TestCase("VsaApp/Features/Twin/TwinExtraModule.cs", "UMP020301", "Feature VsaApp/Features/Twin has 2 Module types")]
    [TestCase("VsaApp/Features/Rogue/RoguePlugin.cs", "UMP020101", "the rule \"Module Under<Feature> denies Composition\" denies it")]
    [TestCase("VsaApp/Plugins/Stray/StrayModule.cs", "UMP020101", "uses Module in Kernel")]
    [TestCase("VsaApp/Plugins/Stray/StrayModule.cs", "UMP020301", "Plugin VsaApp/Plugins/Stray has 0 Module types")]
    [TestCase("VsaApp/Plugins/Tools/Infrastructure/NoteReader.cs", "UMP020101", "the rule \"Plugin denies Feature\" denies it")]
    public void The_VSA_app_gives_the_finding_of_each_case(string file, string code, string text) {
        Assert.That(Findings, Has.Some.StartsWith(file).And.Contains($"error {code}: ").And.Contains(text));
    }

    [TestCase(Notebook + "Slices/Shared/NoteTitle.cs")]
    [TestCase(Notebook + "Slices/GetNote/")]
    [TestCase(Notebook + "Slices/ReadNote/")]
    [TestCase("VsaApp/Plugins/Tools/Infrastructure/ToolClock.cs")]
    [TestCase("VsaApp/Plugins/Clock/")]
    public void A_file_that_follows_the_rules_gives_no_finding(string file) {
        Assert.That(Findings, Has.None.StartsWith(file));
    }

    [Test]
    public void The_VSA_app_gives_only_the_known_findings() {
        var actual = Findings.Select(line => line[..(line.IndexOf("error ", StringComparison.Ordinal) + "error UMP000000".Length)]);

        Assert.That(actual, Is.EquivalentTo(Known));
    }
}

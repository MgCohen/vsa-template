namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class SnapshotSaveTests {
    [Test]
    public void A_build_compile_saves_its_snapshot_in_its_own_folder_with_its_root_and_target_and_reports_no_type_finding() {
        using var build = Wrong();
        File.Delete(build.State.SnapshotFile("Data"));
        var file = Path.Combine(build.Block, "obj", "Data", "umpire", "snapshot.json");
        build.Options["build_property.UmpireSnapshot"] = file;
        build.Options["build_property.TargetFramework"] = "net8.0";
        build.Options["build_property.ProjectDir"] = Path.Combine(build.Block, "Data") + Path.DirectorySeparatorChar;

        var diagnostics = build.Analyze(build.Compile("Data", [build.Compile("Ui", "Ui/Screen.cs")], "Data/Store.cs"));

        var saved = SnapshotText.Read(File.ReadAllText(file));
        Assert.Multiple(() => {
            Assert.That(diagnostics.Select(d => d.Id), Has.None.EqualTo(RuleCodes.DeniedUse));
            Assert.That(saved.Root, Is.EqualTo(Path.Combine(build.Block, "Data")));
            Assert.That(saved.Target, Is.EqualTo("net8.0"));
            Assert.That(saved.Files.Select(f => f.Path), Is.EqualTo(new[] { "Store.cs" }));
            Assert.That(File.Exists(build.State.SnapshotFile("Data")), Is.False);
        });
    }

    [Test]
    public void An_editor_compile_finds_the_state_of_the_last_build_by_the_pointer_next_to_its_snapshot() {
        using var build = Wrong();
        var moved = StateFolder.At(Path.Combine(build.Block, "other", "state"));
        Directory.CreateDirectory(Path.GetDirectoryName(moved.Folder)!);
        Directory.Move(build.State.Folder, moved.Folder);
        var file = Path.Combine(build.Block, "obj", "Data", "umpire", "snapshot.json");
        StatePointer.Write(file, moved);
        build.Options["build_property.UmpireSnapshot"] = file;
        build.Options["build_property.UmpireSaveSnapshot"] = "false";

        var diagnostics = build.Analyze(build.Compile("Data", [build.Compile("Ui", "Ui/Screen.cs")], "Data/Store.cs"));

        Assert.That(diagnostics.Select(d => d.Id), Has.One.EqualTo(RuleCodes.DeniedUse));
    }

    [Test]
    public void A_finding_that_two_targets_give_is_in_the_report_one_time() {
        using var build = Wrong();
        var data = build.Saved("Data");
        var first = Path.Combine(build.Block, "obj", "net8.0", "snapshot.json");
        var second = Path.Combine(build.Block, "obj", "net10.0", "snapshot.json");
        StateFolder.SaveSnapshot(first, data with { Target = "net8.0" });
        StateFolder.SaveSnapshot(second, data with { Target = "net10.0" });
        build.Save(Samples.Layers, build.State.ReadBlock() with {
            Snapshots = [new SnapshotPlace("Data", first, "net8.0"), new SnapshotPlace("Data", second, "net10.0")],
        });

        var report = StateCheck.Run(build.State);

        Assert.Multiple(() => {
            Assert.That(report.Findings.Where(f => f.Code == RuleCodes.DeniedUse), Has.Exactly(1).Items);
            Assert.That(report.Metadata["targets"], Is.EqualTo("net10.0, net8.0"));
        });
    }

    static SampleBuild Wrong() {
        var build = new SampleBuild("Ui", "Data");
        build.Write("Ui/Screen.cs", "namespace Sample;\npublic class Screen;\n");
        build.Write("Data/Store.cs", "namespace Sample;\npublic class Store {\n    public object Make() => new Screen();\n}\n");
        var ui = build.Compile("Ui", "Ui/Screen.cs");
        build.Analyze(ui);
        build.Analyze(build.Compile("Data", [ui], "Data/Store.cs"));
        return build;
    }
}

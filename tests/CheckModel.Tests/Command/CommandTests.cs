using Tests.Shared;

namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class CommandTests {
    static readonly FileFacts screen = Samples.File("Ui/Screen.cs", Samples.Type("Screen"));
    static readonly FileFacts store = Samples.File("Data/Store.cs", Samples.Type("Store", Samples.Use("Screen", 12)));

    [Test]
    public void A_clean_app_gives_no_line_and_the_exit_code_0() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen);

        var result = Run("--root", temp.Block);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Output, Does.Not.Contain("UMP"));
    }

    [Test]
    public void A_finding_is_a_line_with_the_file_the_line_and_the_code() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen, store);

        var result = Run("--root", temp.Block);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Output, Does.Contain(
            $"{Path.Combine(temp.Block, "Data", "Store.cs")}(12,9): error UMP020101: Store in Data (Data) uses Screen in Ui (Ui): "));
    }

    [Test]
    public void A_changed_rule_is_found_with_no_compile() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen);
        temp.State.Save(
            Samples.Layers with { Parts = [Samples.Part(Samples.Data, "Data"), Samples.Part(Samples.Ui, "Screens")] }, Samples.Settings("Sample"));

        Assert.That(Run("--root", temp.Block).Output, Does.Contain("error UMP020202: Screen is in a folder"));
    }

    [Test]
    public void An_old_and_a_missing_snapshot_are_warnings() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen with { Hash = "0" });
        temp.State.Save(Samples.Layers, Samples.Settings("Sample", "Missing"));

        var result = Run("--root", temp.Block);

        Assert.That(result.ExitCode, Is.Zero);
        Assert.That(result.Output, Does.Contain("warning UMP030803").And.Contain("warning UMP030802"));
    }

    [Test]
    public void A_job_that_does_not_run_to_its_end_is_an_error_with_its_reason() {
        using var temp = new TempState();

        var result = Run("--root", temp.Block);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Output, Does.Contain("umpire-check: error UMP030801: The check did not run to its end: "));
    }

    [Test]
    public void The_config_key_and_the_flag_give_the_state_folder() {
        using var temp = new TempState();
        var state = StateFolder.Of(temp.Block, "Library/Umpire");
        state.Save(Samples.Layers, Samples.Settings("Sample"));
        state.Save(Snapshot.Of("Sample", [screen]));
        Assert.That(Run("--root", temp.Block, "--state", state.Folder).ExitCode, Is.Zero);

        File.WriteAllText(Path.Combine(temp.Block, ".umpireconfig"), $"{StateFolder.Key} = \"Library/Umpire\"");

        Assert.That(Run("--root", temp.Block).ExitCode, Is.Zero);
    }

    [Test]
    public void A_bad_flag_gives_the_usage() =>
        Assert.That(Run("--other", "x").ExitCode, Is.EqualTo(2));

    static void Save(TempState temp, Definition definition, params FileFacts[] files) {
        temp.Load(definition, files);
        temp.State.Save(definition, Samples.Settings("Sample"));
    }

    static DotnetResult Run(params string[] args) =>
        Dotnet.Run(AppContext.BaseDirectory, null, ["exec", Path.Combine(AppContext.BaseDirectory, "umpire-check.dll"), .. args]);
}

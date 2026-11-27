namespace CheckModel.Tests;

using Engine.Tests;
using Jobs.Slices.CheckJob;
using Microsoft.Extensions.DependencyInjection;
using Operations.Domain.Shared;
using Umpire.CheckModel;
using Umpire.Vsa.Contracts.Requests;
using UmpireEngine;

public sealed class ModelJobTests {
    static readonly FileFacts screen = Samples.File("Ui/Screen.cs", Samples.Type("Screen"));
    static readonly FileFacts store = Samples.File("Data/Store.cs", Samples.Type("Store", Samples.Use("Screen", 12)));

    [Test]
    public async Task A_clean_app_gives_no_line_and_the_exit_code_0() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen);

        var result = await Run(temp.Block);

        Assert.That(result.ExitCode, Is.Zero, string.Join("\n", result.Lines));
        Assert.That(result.Lines, Has.None.Contains("UMP"));
    }

    [Test]
    public async Task A_finding_is_a_line_with_the_file_the_line_and_the_code() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen, store);

        var result = await Run(temp.Block);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.Some.StartsWith(
            $"{Path.Combine(temp.Block, "Data", "Store.cs")}(12,9): error UMP020101: Store in Data (Data) uses Screen in Ui (Ui): "));
    }

    [Test]
    public async Task A_changed_rule_is_found_with_no_compile() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen);
        temp.State.Save(
            Samples.Layers with { Parts = [Samples.Part(Samples.Data, "Data"), Samples.Part(Samples.Ui, "Screens")] }, Samples.Settings("Sample"));

        Assert.That((await Run(temp.Block)).Lines, Has.Some.Contains("error UMP020202: Screen is in a folder"));
    }

    [Test]
    public async Task An_old_and_a_missing_snapshot_are_warnings() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen with { Hash = "0" });
        temp.State.Save(Samples.Layers, Samples.Settings("Sample", "Missing"));

        var result = await Run(temp.Block);

        Assert.That(result.Lines, Has.Some.Contains("warning UMP030803").And.Some.Contains("warning UMP030802"));
    }

    [Test]
    public async Task A_check_that_does_not_run_to_its_end_gives_its_reason() {
        using var temp = new TempState();

        var result = await Run(temp.Block);

        Assert.That(result.Lines, Has.Some.StartsWith("umpire-engine: warning UMP030801: The check did not run to its end: "));
    }

    [Test]
    public async Task A_check_that_does_not_run_to_its_end_is_an_error_in_Error_mode() {
        using var temp = new TempState();
        File.WriteAllText(Path.Combine(temp.Block, ".umpireconfig"), "umpire.check.mode = \"Error\"");

        Assert.That((await Run(temp.Block)).Lines, Has.Some.StartsWith("umpire-engine: error UMP030801: "));
        Assert.That((await Run(temp.Block, mode: ReportMode.Warn)).Lines, Has.Some.StartsWith("umpire-engine: warning UMP030801: "));
    }

    [Test]
    public async Task A_finding_in_Warn_mode_is_a_warning() {
        using var temp = new TempState();
        Save(temp, Samples.Layers, screen, store);

        var result = await Run(temp.Block, mode: ReportMode.Warn);

        Assert.That(result.Lines, Has.Some.Contains("warning UMP020101").And.None.Contains("error UMP"));
    }

    [Test]
    public async Task The_config_key_and_the_option_give_the_state_folder() {
        using var temp = new TempState();
        var state = StateFolder.Of(temp.Block, "Library/Umpire");
        state.Save(Samples.Layers, Samples.Settings("Sample"));
        state.Save(Snapshot.Of("Sample", [screen]));
        Assert.That((await Run(temp.Block, state.Folder)).ExitCode, Is.Zero);

        File.WriteAllText(Path.Combine(temp.Block, ".umpireconfig"), $"{StateFolder.Key} = \"Library/Umpire\"");

        Assert.That((await Run(temp.Block)).ExitCode, Is.Zero);
    }

    static void Save(TempState temp, Definition definition, params FileFacts[] files) {
        temp.Load(definition, files);
        temp.State.Save(definition, Samples.Settings("Sample"));
    }

    static async Task<ConsoleOutput> Run(string block, string state = "", ReportMode? mode = null) {
        using var host = new EngineTestApp(RunEngineTests.EngineModules()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var requests = scope.ServiceProvider.GetRequiredService<IRequestSender>();
        var check = new CheckJobRequest(block, block, block, mode, Model: true, State: state);
        return ConsoleReport.Of((await requests.SendAsync(check, default)).Value);
    }
}

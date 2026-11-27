using Tests.Shared;

namespace CheckModel.Tests;

using Engine.Tests;
using Jobs.Slices.BuildJob;
using Microsoft.Extensions.DependencyInjection;
using Operations.Contracts.Requests;
using Operations.Domain.Shared;
using Umpire.CheckModel;
using Umpire.Vsa.Contracts.Requests;

public sealed class ReferenceFolderJobTests {
    const string App = "Notes";

    static readonly string configuration =
        Path.GetFileName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    static readonly string Block = Repo.Path("tests", "Fixtures", App);

    [Test]
    public async Task The_build_job_checks_the_types_of_an_app_DLL_in_a_reference_folder() {
        using var source = FixtureLock.Take(App);
        var root = TempFolder.Create();
        try {
            var copy = Copy(root);

            var report = await Run(copy);

            var model = AppModel.Load(StateFolder.At(copy.State));
            Assert.Multiple(() => {
                Assert.That(report.Findings.Select(f => f.Code), Has.None.EqualTo(DataCodes.NoSnapshot).And.None.EqualTo(DataCodes.NoModel));
                Assert.That(File.Exists(StateFolder.At(copy.State).SnapshotFile(App)), Is.True);
                Assert.That(model.TypesOf(App).Select(t => t.Name.FullName), Has.Some.StartsWith("Notes.Features.Notebook."));
                Assert.That(model.Types.Where(t => t.Project == App && t.IsUnmapped), Is.Empty);
            });
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task A_use_in_a_DLL_of_a_reference_folder_is_checked() {
        using var source = FixtureLock.Take(App);
        var root = TempFolder.Create();
        try {
            var copy = Copy(root);

            await Run(copy);
            var model = AppModel.Load(StateFolder.At(copy.State));
            var handler = model.TypesOf(App).First(t => t.Name.Name == "CreateNoteHandler");

            Assert.That(handler.References.Select(r => r.Fact.Target.Name), Has.Some.EqualTo("Note"));
            Assert.That(model.PlacementOf(handler)?.Part.Name, Is.EqualTo("Handlers.Requests"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    static Copied Copy(string root) {
        var from = Repo.Path("tests", "Fixtures", App, "artifacts~", "bin", App, configuration);
        var bin = Path.Combine(root, "bin");
        var scripts = Path.Combine(root, "Scripts");
        TempFolder.Copy(from, bin);
        Directory.CreateDirectory(scripts);
        foreach (var file in new[] { App + ".dll", App + ".pdb" }) {
            File.Move(Path.Combine(bin, file), Path.Combine(scripts, file));
        }
        File.Delete(Path.Combine(bin, App + ".deps.json"));
        return new Copied(bin, scripts, Path.Combine(root, "state"));
    }

    static async Task<JobReport> Run(Copied copy) {
        using var host = new EngineTestApp(RunEngineTests.EngineModules()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var requests = scope.ServiceProvider.GetRequiredService<IRequestSender>();
        var request = new BuildJobRequest(Block, copy.Bin, Block, ReportMode.Error, References: copy.Scripts, State: copy.State);
        return (await requests.SendAsync(request, default)).Value;
    }

    sealed record Copied(string Bin, string Scripts, string State);
}

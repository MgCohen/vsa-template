using Facts.Domain.Shared.Compile;
using Jobs.Slices.CompileJob;
using Microsoft.CodeAnalysis.CSharp;
using Umpire.Kernels;
using Umpire.Vsa.Composition;
using UmpireEngine;

namespace Engine.Tests;

[NonParallelizable]
public sealed class CompileJobTests {
    CompileHost app = null!;
    AppInstance running = null!;

    [OneTimeSetUp]
    public void Start() {
        app = new CompileHost();
        running = new EngineApp().Run(app);
    }

    [OneTimeTearDown]
    public void Stop() {
        running.Stop();
        app.Dispose();
    }

    [Test]
    public void The_compile_job_checks_the_code_of_one_file() {
        var project = Project("umpire.UMP010101.max = 10");
        app.Send(new CompileJobRequest(project, CompileStage.Start));

        var report = app.Send(new CompileJobRequest(project, CompileStage.File, File: File("A.cs", 12)));

        Assert.That(report.Value.Findings.Select(f => (f.Code, f.File, f.Line, f.Column, f.Length)),
            Is.EqualTo(new[] { ("UMP010101", (string?)"A.cs", (int?)1, (int?)11, (int?)2) }));
    }

    [Test]
    public void The_start_of_the_compile_job_gives_the_option_findings() {
        var project = Project("umpire.UMP010101.max = \"many\"");

        var report = app.Send(new CompileJobRequest(project, CompileStage.Start));

        Assert.That(report.Value.Findings.Select(f => f.Code), Is.EqualTo(new[] { "UMP010001" }));
    }

    [Test]
    public void A_config_file_that_is_not_valid_stops_the_code_check() {
        var project = Project("a = 1\n[*.cs]\nb = 2\n");

        var start = app.Send(new CompileJobRequest(project, CompileStage.Start));
        var file = app.Send(new CompileJobRequest(project, CompileStage.File, File: File("A.cs", 500)));

        Assert.That(start.Value.Findings.Select(f => f.Code), Is.EqualTo(new[] { "UMP010002" }));
        Assert.That(file.Value.Findings, Is.Empty);
    }

    [Test]
    public void A_project_with_no_state_of_the_check_model_is_not_in_the_model() {
        var report = app.Send(new CompileJobRequest(Project(""), CompileStage.Start));

        Assert.That(report.Value.InModel, Is.False);
    }

    static CompileProject Project(string config) {
        var project = new CompileProject("Sample", "", config, false, [], [], new ModelSettings(null, null, null, [], false));
        CompileProjects.Add(project, CSharpCompilation.Create("Sample"));
        return project;
    }

    static CodeFile File(string path, int length) => new(path, [length], [], [], [], [], []);
}

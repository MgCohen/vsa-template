using Tests.Shared;
using Umpire.Cli.Features.Checks.Infrastructure;
using Umpire.Cli.Plugins.Commands.Domain.Shared;

namespace Cli.Tests;

public sealed class AppEngineTests {
    static readonly string AnyFile = typeof(AppEngineTests).Assembly.Location;

    [Test]
    public void TheCheckRunsTheEngineOfTheAppWithTheCheckJob() {
        var block = TempFolder.Create();
        try {
            var engine = AppEngine.FromProperties(block, Properties(AnyFile, "Major", "/app/bin/", "/app/"));

            Assert.That(engine.CheckArguments(block, "Error"), Is.EqualTo(new[] {
                "exec", "--roll-forward", "Major", AnyFile, "jobs", "check", "--project", block.TrimEnd('/', '\\'),
                "--bin", Path.GetFullPath("/app/bin"), "--block", Path.GetFullPath("/app"), "--mode", "Error",
            }));
        } finally {
            TempFolder.Delete(block);
        }
    }

    [Test]
    public void WithNoRollForwardTheEngineRunsOnTheNewestRuntime() {
        var engine = AppEngine.FromProperties("/app", "Restored.\n" + Properties(AnyFile, "", "/app/bin/", ""));

        Assert.That(engine.RollForward, Is.EqualTo("LatestMajor"));
        Assert.That(engine.CheckArguments("/app", null), Does.Not.Contain("--block").And.Not.Contain("--mode"));
    }

    [Test]
    public void AnAppWithNoEnginePackageStops() =>
        Assert.That(() => AppEngine.FromProperties("/app", Properties("", "", "/app/bin/", "")),
            Throws.TypeOf<UmpireException>().With.Message.Contain("The app has no rule engine")
                .And.Message.Contain("<PackageReference Include=\"Umpire.Engine\""));

    [Test]
    public void AnEngineThatIsNotRestoredStops() =>
        Assert.That(() => AppEngine.FromProperties("/app", Properties("/no/umpire-engine.dll", "", "/app/bin/", "")),
            Throws.TypeOf<UmpireException>().With.Message.Contain("The rule engine /no/umpire-engine.dll does not exist"));

    [Test]
    public void ARootProjectWithManyTargetsStops() =>
        Assert.That(() => AppEngine.FromProperties("/app", Properties(AnyFile, "", "", "")),
            Throws.TypeOf<UmpireException>().With.Message.Contain("Give the root project one TargetFramework"));

    [Test]
    public void OutputThatIsNotJsonStops() =>
        Assert.That(() => AppEngine.FromProperties("/app", "warn {NU1603} restore"),
            Throws.TypeOf<UmpireException>().With.Message.Contain("MSBuild gave no properties"));

    [Test]
    public void NoPropertiesStop() =>
        Assert.That(() => AppEngine.FromProperties("/app", "error MSB1009: Project file does not exist."),
            Throws.TypeOf<UmpireException>().With.Message.Contain("MSBuild gave no properties"));

    static string Properties(string engine, string rollForward, string bin, string block) => $$"""
        {
          "Properties": {
            "UmpireEngineFile": "{{engine.Replace("\\", "\\\\")}}",
            "UmpireEngineRollForward": "{{rollForward}}",
            "TargetDir": "{{bin}}",
            "UmpireBlock": "{{block}}"
          }
        }
        """;
}

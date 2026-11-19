using System.Reflection;
using Blocks.Slices.FindBlocks;
using Build.Slices.BuildApp;
using LoadApp.Slices.LoadApp;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Tests.Shared;
using Umpire.Engine.Versions;
using Versions.Slices.CheckVersions;

namespace Engine.Tests;

public sealed class CodeTests {
    static readonly Assembly Engine = typeof(CheckRulesCommand).Assembly;

    static IEnumerable<Type> EngineTypes => Engine.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface);

    [Test]
    public void Each_engine_step_gives_its_code_group() {
        var steps = EngineTypes.Where(t => typeof(IEngineStep).IsAssignableFrom(t));

        Assert.That(steps, Has.All.Matches<Type>(t => typeof(IStepGroup).IsAssignableFrom(t)));
    }

    [Test]
    public void The_groups_follow_the_order_of_the_steps() {
        Assert.Multiple(() => {
            Assert.That(BuildAppCommand.Group, Is.EqualTo(1));
            Assert.That(FindBlocksCommand.Group, Is.EqualTo(3));
            Assert.That(CheckVersionsCommand.Group, Is.EqualTo(4));
            Assert.That(LoadAppCommand.Group, Is.EqualTo(5));
            Assert.That(CheckRulesCommand.Group, Is.EqualTo(6));
            Assert.That(Report.Slices.WriteReport.WriteReportCommand.Group, Is.EqualTo(7));
        });
    }

    [Test]
    public void Each_engine_step_gives_its_stages() {
        var steps = EngineTypes.Where(t => typeof(IEngineStep).IsAssignableFrom(t));

        Assert.That(steps, Has.All.Matches<Type>(t => typeof(IStepStage).IsAssignableFrom(t)));
    }

    [Test]
    public void Only_the_build_step_is_in_the_build_stage() {
        Assert.Multiple(() => {
            Assert.That(BuildAppCommand.Stages, Is.EqualTo(Stage.Build));
            Assert.That(FindBlocksCommand.Stages, Is.EqualTo(Stage.Check));
            Assert.That(CheckVersionsCommand.Stages, Is.EqualTo(Stage.Check));
            Assert.That(LoadAppCommand.Stages, Is.EqualTo(Stage.Check));
            Assert.That(CheckRulesCommand.Stages, Is.EqualTo(Stage.Check));
        });
    }

    [Test]
    public void Each_step_has_a_different_group() {
        var groups = EngineTypes.Where(t => typeof(IStepGroup).IsAssignableFrom(t))
            .Select(t => (int)t.GetProperty(nameof(IStepGroup.Group), BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!)
            .ToList();

        Assert.That(groups, Is.Unique.And.None.EqualTo(StepCode.RunGroup));
    }

    [TestCase(nameof(LoadAppCodes.NoAppAssembly), "UMP030501")]
    [TestCase(nameof(LoadAppCodes.NoAppClass), "UMP030502")]
    [TestCase(nameof(LoadAppCodes.ManyAppClasses), "UMP030503")]
    [TestCase(nameof(LoadAppCodes.NoEmptyConstructor), "UMP030504")]
    public void A_LoadApp_code_is_in_group_05(string name, string code) {
        Assert.That(typeof(LoadAppCodes).GetField(name)!.GetValue(null), Is.EqualTo(code));
    }

    [Test]
    public void The_codes_of_each_step_start_with_the_block_and_the_group_of_the_step() {
        Assert.Multiple(() => {
            Assert.That(StepCode.Of<CheckVersionsCommand>(1), Is.EqualTo(VersionCheck.Code));
            Assert.That(BlocksCodes.TwoEntries, Is.EqualTo("UMP030301"));
            Assert.That(BlocksCodes.BadArchitecture, Is.EqualTo("UMP030302"));
            Assert.That(BlocksCodes.NoBlock, Is.EqualTo("UMP030303"));
            Assert.That(BlocksCodes.CodeWins, Is.EqualTo("UMP030304"));
            Assert.That(BlocksCodes.NoUmpireSdk, Is.EqualTo("UMP030305"));
            Assert.That(BlocksCodes.NoConfigFile, Is.EqualTo("UMP030306"));
            Assert.That(BuildCodes.Failed, Is.EqualTo("UMP030101"));
            Assert.That(RunCodes.CouldNotRun, Is.EqualTo("UMP030001"));
            Assert.That(RunCodes.Skipped, Is.EqualTo("UMP030002"));
        });
    }

    [Test]
    public void Each_engine_code_is_different() {
        string[] codes = [
            VersionCheck.Code, RunCodes.CouldNotRun, RunCodes.Skipped, BuildCodes.Failed,
            BlocksCodes.TwoEntries, BlocksCodes.BadArchitecture, BlocksCodes.NoBlock,
            BlocksCodes.CodeWins, BlocksCodes.NoUmpireSdk, BlocksCodes.NoConfigFile,
            LoadAppCodes.NoAppAssembly, LoadAppCodes.NoAppClass, LoadAppCodes.ManyAppClasses, LoadAppCodes.NoEmptyConstructor,
        ];

        Assert.That(codes, Is.Unique.And.All.StartsWith(StepCode.Prefix));
    }
}

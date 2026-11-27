using System.Reflection;
using Blocks.Slices.FindBlocks;
using Build.Slices.BuildApp;
using Changes.Slices.FindChanges;
using LoadApp.Slices.LoadApp;
using Rules.Slices.CheckRules;
using Operations.Domain.Shared;
using Operations.Infrastructure;
using Tests.Shared;
using Umpire.Engine.Versions;
using Versions.Slices.CheckVersions;
using Model.Slices.CheckModel;
using Model.Slices.ExportModel;

namespace Engine.Tests;

public sealed class CodeTests {
    static readonly Assembly Engine = typeof(CheckRulesCommand).Assembly;

    static IEnumerable<Type> EngineTypes => Engine.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface);

    [Test]
    public void Each_operation_gives_its_code_group() {
        var operations = EngineTypes.Where(t => typeof(IJobOperation).IsAssignableFrom(t));

        Assert.That(operations, Has.All.Matches<Type>(t => typeof(IOperationGroup).IsAssignableFrom(t)));
    }

    [Test]
    public void The_groups_follow_the_order_of_the_operations() {
        Assert.Multiple(() => {
            Assert.That(BuildAppCommand.Group, Is.EqualTo(1));
            Assert.That(FindChangesCommand.Group, Is.EqualTo(2));
            Assert.That(FindBlocksCommand.Group, Is.EqualTo(3));
            Assert.That(CheckVersionsCommand.Group, Is.EqualTo(4));
            Assert.That(LoadAppCommand.Group, Is.EqualTo(5));
            Assert.That(CheckRulesCommand.Group, Is.EqualTo(6));
            Assert.That(ExportModelCommand.Group, Is.EqualTo(7));
            Assert.That(CheckModelCommand.Group, Is.EqualTo(8));
            Assert.That(OperationCode.Of<CheckModelCommand>(1), Is.EqualTo(Umpire.CheckModel.DataCodes.NoModel));
        });
    }

    [Test]
    public void Each_operation_has_a_different_group() {
        var groups = EngineTypes.Where(t => typeof(IOperationGroup).IsAssignableFrom(t))
            .Select(OperationCode.GroupOf)
            .ToList();

        Assert.That(groups, Is.Unique.And.None.EqualTo(OperationCode.RunGroup));
    }

    [TestCase(nameof(LoadAppCodes.NoAppAssembly), "UMP030501")]
    [TestCase(nameof(LoadAppCodes.NoAppClass), "UMP030502")]
    [TestCase(nameof(LoadAppCodes.ManyAppClasses), "UMP030503")]
    [TestCase(nameof(LoadAppCodes.NoPackedConstructor), "UMP030505")]
    [TestCase(nameof(LoadAppCodes.ManyKernels), "UMP030506")]
    [TestCase(nameof(LoadAppCodes.OtherKernel), "UMP030507")]
    [TestCase(nameof(LoadAppCodes.ManyDefinitions), "UMP030508")]
    [TestCase(nameof(LoadAppCodes.NoDefinition), "UMP030509")]
    [TestCase(nameof(LoadAppCodes.MissingAssembly), "UMP030510")]
    [TestCase(nameof(LoadAppCodes.IgnoredDefinition), "UMP030511")]
    [TestCase(nameof(LoadAppCodes.EmptyDefinitionFolder), "UMP030512")]
    [TestCase(nameof(LoadAppCodes.UnloadedDefinition), "UMP030513")]
    public void A_LoadApp_code_is_in_group_05(string name, string code) {
        Assert.That(typeof(LoadAppCodes).GetField(name)!.GetValue(null), Is.EqualTo(code));
    }

    [Test]
    public void The_codes_of_each_operation_start_with_the_block_and_the_group_of_the_operation() {
        Assert.Multiple(() => {
            Assert.That(OperationCode.Of<CheckVersionsCommand>(1), Is.EqualTo(VersionCheck.Code));
            Assert.That(BlocksCodes.TwoEntries, Is.EqualTo("UMP030301"));
            Assert.That(BlocksCodes.BadArchitecture, Is.EqualTo("UMP030302"));
            Assert.That(BlocksCodes.NoBlock, Is.EqualTo("UMP030303"));
            Assert.That(BlocksCodes.CodeWins, Is.EqualTo("UMP030304"));
            Assert.That(BlocksCodes.NoUmpirePackage, Is.EqualTo("UMP030305"));
            Assert.That(BlocksCodes.NoConfigFile, Is.EqualTo("UMP030306"));
            Assert.That(BlocksCodes.NewerRuntime, Is.EqualTo("UMP030307"));
            Assert.That(BuildCodes.Failed, Is.EqualTo("UMP030101"));
            Assert.That(ChangesCodes.Same, Is.EqualTo("UMP030201"));
            Assert.That(ChangesCodes.BlockSame, Is.EqualTo("UMP030202"));
            Assert.That(RunCodes.CouldNotRun, Is.EqualTo("UMP030001"));
            Assert.That(RunCodes.Skipped, Is.EqualTo("UMP030002"));
            Assert.That(RunCodes.NoEngine, Is.EqualTo("UMP030003"));
            Assert.That(RunCodes.EngineCrash, Is.EqualTo("UMP030004"));
        });
    }

    [Test]
    public void The_code_list_of_the_SDK_has_each_engine_code_and_no_other() {
        var steps = Engine.GetTypes()
            .Where(t => t.Name.EndsWith("Codes", StringComparison.Ordinal) && t != typeof(Umpire.CheckModel.RuleCodes))
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!);
        var rules = ArchitectureRule.All.Select(rule => rule.Code);

        var listed = SdkCodes.All.Where(code => code[3..5] is "02" or "03");

        Assert.That(listed, Is.EquivalentTo(steps.Concat(rules).Append(VersionCheck.Code)));
    }

    [Test]
    public void Each_engine_code_is_different() {
        string[] codes = [
            VersionCheck.Code, RunCodes.CouldNotRun, RunCodes.Skipped, RunCodes.NoEngine, RunCodes.EngineCrash, BuildCodes.Failed, ChangesCodes.Same,
            BlocksCodes.TwoEntries, BlocksCodes.BadArchitecture, BlocksCodes.NoBlock,
            BlocksCodes.CodeWins, BlocksCodes.NoUmpirePackage, BlocksCodes.NoConfigFile,
            LoadAppCodes.NoAppAssembly, LoadAppCodes.NoAppClass, LoadAppCodes.ManyAppClasses,
            LoadAppCodes.NoPackedConstructor, LoadAppCodes.ManyKernels,
            LoadAppCodes.OtherKernel, LoadAppCodes.ManyDefinitions, LoadAppCodes.NoDefinition, LoadAppCodes.MissingAssembly, LoadAppCodes.IgnoredDefinition, LoadAppCodes.EmptyDefinitionFolder, LoadAppCodes.UnloadedDefinition,
            BlocksCodes.NoDefinitionProject,
        ];

        Assert.That(codes, Is.Unique.And.All.StartsWith(OperationCode.Prefix));
    }
}

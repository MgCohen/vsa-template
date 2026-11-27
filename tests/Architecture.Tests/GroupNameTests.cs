namespace EngineCore.Tests;

extern alias testing;

using Umpire.Architecture;
using testing::Umpire.Architecture;

public sealed class GroupNameTests : ArchitectureTest<GroupNameTests.ModulesArchitecture> {
    [Test]
    public void A_group_name_in_a_plugin_module_takes_the_plugin_name() {
        Assert.That(Problems(At(typeof(Journal), "Plugins/Journal/Cli")), Is.Empty);
    }

    [Test]
    public void A_wrong_name_in_a_plugin_module_gives_the_plugin_name() {
        Assert.That(Problems(At(typeof(Diary), "Plugins/Journal/Cli")), Has.Some.Contains("Rename it to Journal"));
    }

    [Test]
    public void A_group_name_in_a_feature_module_takes_the_feature_name() {
        Assert.That(Problems(At(typeof(Billing), "Features/Billing/Cli")), Is.Empty);
    }

    public static class Journal;

    public static class Diary;

    public static class Billing;

    public record FeatureModule : Concept {
        public override string GroupFolder => "Features";
    }

    public record PluginModule : Concept {
        public override string GroupFolder => "Plugins";
    }

    public record Cli : Concept;

    public sealed class ModulesArchitecture : Architecture {
        protected override void Define(Part block, Part kernel) {
            block.AddGroup<FeatureModule>().Add<Cli>().NamedAfterGroup();
            block.AddGroup<PluginModule>().Add<Cli>().NamedAfterGroup();
        }
    }
}

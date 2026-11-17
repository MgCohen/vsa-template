namespace Rules.Slices.CheckRules;

using BuiltApp.Infrastructure;
using Rules.Domain;
using Steps.Domain.Shared;

sealed class MissingScriptAssembliesRule : PlaceRule {
    public override int Number => 3;

    public override FindingSeverity Severity => FindingSeverity.Warning;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) {
        if (!check.App.MissingScriptAssemblies) {
            yield break;
        }
        yield return At((string?)null,
            $"{check.App.BlockFolder} is a Unity project, but it has no {UnityProject.ScriptAssembliesFolder} folder. "
            + "The checks read only the .NET build, so they do not check the code that only Unity compiles. "
            + "Open the project in Unity to compile its scripts, then build again.");
    }
}

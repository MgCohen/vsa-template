namespace Rules.Slices.CheckRules;

using BuiltApp.Infrastructure;
using Rules.Domain;
using Steps.Domain.Shared;

sealed class BadFolderAliasRule : PlaceRule {
    public override int Number => 6;

    public override FindingSeverity Severity => FindingSeverity.Warning;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.BadFolderAliases.Select(key => At((string?)null,
            $"The folder alias {key} in {FolderAliasConfig.Key} is not a list of folders, so the checks do not use it. "
            + $"Write it as \"{key}\" = [\"\", \"App\"], with quotes on the key."));
}

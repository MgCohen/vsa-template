namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class EmptyBlockPlaceRule : BlockPlaceRule {
    public override int Number => 2;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.EmptyPlaces(check.App.BlockFolder, check.InnerFolders).Select(place => At(place.Folder,
            $"{place.Part.Name} ({place.Part.Path}) marks the folder {place.InBlock} as the place of a block, "
            + "but no check entry is in it. Add a check entry to the folder, or remove the place from Define."));
}

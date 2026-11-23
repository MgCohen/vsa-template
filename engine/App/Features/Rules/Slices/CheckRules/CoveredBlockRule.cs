namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;

sealed class CoveredBlockRule : BlockPlaceRule {
    public override int Number => 1;

    protected override IEnumerable<Finding> Find(ArchitectureCheck check, RuleValues values) =>
        check.Resolved.CoveredBlocks(check.App.BlockFolder, check.InnerFolders).Select(place => At(place.Folder,
            $"{place.Part.Name} ({place.Part.Path}) covers the folder {place.InBlock}, "
            + $"but a check entry makes {place.InBlock} another block. Mark the folder with AddBlock<T>() "
            + "or AddBlockGroup<T>() in Define, or remove the check entry of the inner block."));
}

namespace Umpire.CheckModel;

public static class RuleCodes {
    public const string DeniedUse = "UMP020101";
    public const string WideExposure = "UMP020102";
    public const string WrongType = "UMP020201";
    public const string UnmappedType = "UMP020202";
    public const string WrongName = "UMP020204";
    public const string MissingReference = "UMP020205";
    public const string BadFolderAlias = "UMP020206";
    public const string Count = "UMP020301";
    public const string Match = "UMP020302";
    public const string WrongTrait = "UMP020303";
    public const string TypePlace = "UMP020304";
    public const string CoveredBlock = "UMP020401";
    public const string EmptyBlockPlace = "UMP020402";

    public static IReadOnlyList<string> All { get; } = [
        DeniedUse, WideExposure, WrongType, UnmappedType, WrongName, Count, Match, WrongTrait, TypePlace, CoveredBlock, EmptyBlockPlace,
    ];
}

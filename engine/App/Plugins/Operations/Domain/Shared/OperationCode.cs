namespace Operations.Domain.Shared;

public static class OperationCode {
    public const string Prefix = "UMP03";
    public const int RunGroup = 0;

    public static string Of<TOperation>(int number) where TOperation : IOperationGroup =>
        Format(TOperation.Group, number);

    public static string Run(int number) => Format(RunGroup, number);

    static string Format(int group, int number) => $"{Prefix}{group:00}{number:00}";
}

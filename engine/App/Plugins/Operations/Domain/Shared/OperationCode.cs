namespace Operations.Domain.Shared;

using System.Reflection;

public static class OperationCode {
    public const string Prefix = "UMP03";
    public const int RunGroup = 0;
    public const string GroupProperty = "Group";

    public static string Of<TOperation>(int number) where TOperation : IOperationGroup =>
        Format(GroupOf(typeof(TOperation)), number);

    public static int GroupOf(Type operation) =>
        operation.GetProperty(GroupProperty, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is int group
            ? group
            : throw new InvalidOperationException(
                $"{operation.Name} is an {nameof(IOperationGroup)} with no 'public static int {GroupProperty}'. Add it.");

    public static string Run(int number) => Format(RunGroup, number);

    static string Format(int group, int number) => $"{Prefix}{group:00}{number:00}";
}

namespace Steps.Domain.Shared;

public static class StepCode {
    public const string Prefix = "UMP03";
    public const int RunGroup = 0;

    public static string Of<TStep>(int number) where TStep : IStepGroup => Format(TStep.Group, number);

    public static string Run(int number) => Format(RunGroup, number);

    static string Format(int group, int number) => $"{Prefix}{group:00}{number:00}";
}

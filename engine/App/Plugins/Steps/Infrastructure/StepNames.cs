namespace Steps.Infrastructure;

static class StepNames {
    const string Suffix = "Command";

    public static string Of(object step) => Of(step.GetType());

    public static string Of(Type step) {
        var name = step.Name;
        return name.EndsWith(Suffix, StringComparison.Ordinal) ? name[..^Suffix.Length] : name;
    }
}

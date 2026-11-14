namespace Steps.Infrastructure;

static class StepNames {
    const string Suffix = "Command";

    public static string Of(object step) {
        var name = step.GetType().Name;
        return name.EndsWith(Suffix, StringComparison.Ordinal) ? name[..^Suffix.Length] : name;
    }
}

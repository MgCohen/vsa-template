namespace Umpire.Architecture;

internal static class Limits {
    public static string Need(int? min, int? max, bool oneKind) {
        var parts = new List<string>();
        if (min is not null && min == max) {
            parts.Add($"exactly {min}");
        } else {
            if (min is not null) {
                parts.Add($"at least {min}");
            }
            if (max is not null) {
                parts.Add($"at most {max}");
            }
        }
        if (oneKind) {
            parts.Add("of one kind");
        }
        return string.Join(", ", parts);
    }
}

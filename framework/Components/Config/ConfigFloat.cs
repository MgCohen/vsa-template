namespace Umpire.Config;

using System.Globalization;

sealed class ConfigFloat {
    public ConfigFloat(double value, string text) {
        Value = value;
        Text = text;
    }

    public double Value { get; }

    public string Text { get; }

    public bool IsFinite => !double.IsInfinity(Value) && !double.IsNaN(Value);

    public static string Format(double value) =>
        double.IsNaN(value) ? "nan"
            : double.IsPositiveInfinity(value) ? "inf"
            : double.IsNegativeInfinity(value) ? "-inf"
            : value.ToString("R", CultureInfo.InvariantCulture);
}

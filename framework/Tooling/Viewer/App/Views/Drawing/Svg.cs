using System.Globalization;

namespace App.Views.Drawing;

public static class Svg
{
    public static string Format(FormattableString text)
    {
        return text.ToString(CultureInfo.InvariantCulture);
    }

    public static string N(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}

namespace App.Views.Drawing;

public static class RoundedPath
{
    public static string Build(IReadOnlyList<double[]> points, double radius)
    {
        string d = Svg.Format($"M{points[0][0]},{points[0][1]}");
        for (int k = 1; k < points.Count - 1; k++)
        {
            d += Corner(points[k - 1], points[k], points[k + 1], radius);
        }
        double[] last = points[^1];
        return d + Svg.Format($"L{last[0]},{last[1]}");
    }

    private static string Corner(double[] p0, double[] p1, double[] p2, double radius)
    {
        double l1 = Math.Max(Math.Sqrt(Math.Pow(p1[0] - p0[0], 2) + Math.Pow(p1[1] - p0[1], 2)), 0.001);
        double l2 = Math.Max(Math.Sqrt(Math.Pow(p2[0] - p1[0], 2) + Math.Pow(p2[1] - p1[1], 2)), 0.001);
        double r = Math.Min(radius, Math.Min(l1 / 2, l2 / 2));
        double ax = p1[0] - (p1[0] - p0[0]) / l1 * r;
        double ay = p1[1] - (p1[1] - p0[1]) / l1 * r;
        double bx = p1[0] + (p2[0] - p1[0]) / l2 * r;
        double by = p1[1] + (p2[1] - p1[1]) / l2 * r;
        return Svg.Format($"L{ax},{ay}Q{p1[0]},{p1[1]} {bx},{by}");
    }
}

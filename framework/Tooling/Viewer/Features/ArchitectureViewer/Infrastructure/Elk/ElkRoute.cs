using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkRoute(string Id, string? Container, IReadOnlyList<ElkSection> Sections)
{
    public IReadOnlyList<double[]> Points(Offset origin)
    {
        ElkSection section = Sections[0];
        return [.. (section.BendPoints ?? []).Prepend(section.StartPoint).Append(section.EndPoint)
            .Select(p => new[] { Round(p.X + origin.X), Round(p.Y + origin.Y) })];
    }

    private static double Round(double value)
    {
        return Math.Round(value * 10, MidpointRounding.AwayFromZero) / 10;
    }
}

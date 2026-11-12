using Viewer.Export.Model;
using VSA.Architecture;

namespace Viewer.Export;

internal static class RuleTerms
{
    public static TermData Of(Entry entry)
    {
        List<FilterData> filters = [.. entry.Filters.Select(f => new FilterData(f.Kind, Layer.Display(f.Target)))];
        return new TermData(Layer.Display(entry.Target), entry.Reach?.ToString(), entry.Access?.ToString(), filters);
    }
}

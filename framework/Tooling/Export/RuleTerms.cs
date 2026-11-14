using Viewer.Export.Model;
using VSA.Architecture;

namespace Viewer.Export;

internal static class RuleTerms
{
    public static TermData Of(Entry entry)
    {
        List<FilterData> filters = [.. entry.Filters.Select(f => new FilterData(f.Kind, Concept.Display(f.Target)))];
        return new TermData(Concept.Display(entry.Target), entry.Depth?.ToString(), entry.Access?.ToString(), filters);
    }
}

using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Definitions;

public static class TermText
{
    public static string Of(Term term)
    {
        return string.Join(" ", term.Conditions.Select(Of).Prepend(term.Layer));
    }

    public static string Of(Condition condition)
    {
        return $"{condition.Kind}<{condition.Layer}>";
    }
}

using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Definitions;

public static class DraftTerms
{
    public static Term From(string layer, string condition, string conditionLayer)
    {
        return new(layer, condition == Condition.Under ? [new(condition, conditionLayer)] : []);
    }

    public static Term To(string layer, string condition, string conditionLayer)
    {
        bool onTarget = condition is Condition.InOwn or Condition.InOther;
        return new(layer, onTarget ? [new(condition, conditionLayer)] : []);
    }
}

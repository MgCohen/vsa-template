namespace Cards.Presenters;

using Cards.Domain.Shared;
using Cards.Slices.PayCost;
using Resources.Domain.Shared;

public static class CostLabel {
    public static string Of(Cost cost) => $"{cost.Amount} {cost.Resource}";

    public static string Of(ResourceKey resource, int amount) => $"{amount} {resource.Name}";

    public static string Of(CostPaid paid) => "Paid " + Of(paid.Cost);
}

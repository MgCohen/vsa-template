using Cards.Domain.Shared;
using Cards.Slices.PayCost;

namespace Cards.Presenters;

// A presenter type: it reads the own published domain, the listed Resources key, and an event of its own module.
public static class CostLabel {
    public static string Of(Cost cost) => $"{cost.Amount} {cost.Resource.Name}";

    public static string Of(CostPaid paid) => "Paid " + Of(paid.Cost);
}

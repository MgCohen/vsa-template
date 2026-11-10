using GoodApp.Modules.Billing.Domain.Shared;
using VSA.Domain;

namespace GoodApp.Modules.Billing.Domain;

public sealed class Invoice : AggregateRoot<InvoiceId>
{
    Invoice() { }

    public decimal Amount { get; private set; }
    public bool Paid { get; private set; }

    public static Invoice Create(decimal amount) => new() { Id = new(Guid.NewGuid()), Amount = amount };

    public void Pay() => Paid = true;
}

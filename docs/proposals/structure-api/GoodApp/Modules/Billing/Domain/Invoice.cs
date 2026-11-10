using GoodApp.Modules.Billing.Domain.Shared;

namespace GoodApp.Modules.Billing.Domain;

public class Invoice(InvoiceId id)
{
    public InvoiceId Id => id;
}

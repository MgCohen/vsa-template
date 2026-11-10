using BadApp.Modules.Billing.Domain.Shared;
using BadApp.Modules.Billing.Infrastructure;

namespace BadApp.Modules.Billing.Domain;

public class Invoice(InvoiceId id)
{
    public InvoiceId Id => id;
    public void SaveWith(InvoiceStore store) => store.Save(this);
}

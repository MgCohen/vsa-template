using GoodApp.Modules.Billing.Domain;
using GoodApp.Modules.Billing.Domain.Shared;
using GoodApp.Modules.Billing.Infrastructure;

namespace GoodApp.Modules.Billing.Slices.PayInvoice;

public record PayInvoiceRequest(InvoiceId Id) : Fw.IRequest;

public class PayInvoiceHandler(InvoiceStore store) : Fw.Handler<PayInvoiceRequest>
{
    public override void Handle(PayInvoiceRequest request) => store.Save(new Invoice(request.Id));
}

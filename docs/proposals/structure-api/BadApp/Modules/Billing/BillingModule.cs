namespace BadApp.Modules.Billing;

public class BillingModule : Fw.ModuleBase
{
    public Type[] Handlers => [typeof(Slices.PayInvoice.PayInvoiceHandler), typeof(Infrastructure.InvoiceStore)];
}

using BadApp.Billing.Domain;

namespace BadApp.Billing.Presenters;

public sealed class InvoicePresenter : Kit.IPresenter {
    public Invoice? Shown { get; set; }
}

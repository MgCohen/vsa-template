using GoodApp.Billing.Domain;

namespace GoodApp.Billing.Presenters;

public sealed class InvoicePresenter : Kit.IPresenter {
    public Invoice? Shown { get; set; }
}

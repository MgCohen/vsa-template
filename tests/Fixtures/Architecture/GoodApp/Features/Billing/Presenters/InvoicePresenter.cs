using GoodApp.Billing.Domain;

namespace GoodApp.Billing.Presenters;

public sealed class InvoicePresenter : Kit.IPresenter {
    internal Invoice? Shown { get; set; }
}

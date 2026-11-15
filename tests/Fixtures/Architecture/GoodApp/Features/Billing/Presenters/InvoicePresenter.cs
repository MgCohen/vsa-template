namespace GoodApp.Billing.Presenters;

using GoodApp.Billing.Domain;

public sealed class InvoicePresenter : Kit.IPresenter {
    internal Invoice? Shown { get; set; }
}

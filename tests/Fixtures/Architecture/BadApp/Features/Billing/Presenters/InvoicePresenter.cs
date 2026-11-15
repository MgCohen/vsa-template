namespace BadApp.Billing.Presenters;

using BadApp.Billing.Domain;

public sealed class InvoicePresenter : Kit.IPresenter {
    public Invoice? Shown { get; set; }
}

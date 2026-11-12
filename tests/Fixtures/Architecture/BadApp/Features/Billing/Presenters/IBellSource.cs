using BadApp.Billing.Slices.CreateInvoice;

namespace BadApp.Billing.Presenters;

internal interface IBellSource : Kit.IPresenter {
    InvoiceCreated? Latest { get; }
}

namespace BadApp.Billing.Presenters;

using BadApp.Billing.Slices.CreateInvoice;

internal interface IBellSource : Kit.IPresenter {
    InvoiceCreated? Latest { get; }
}

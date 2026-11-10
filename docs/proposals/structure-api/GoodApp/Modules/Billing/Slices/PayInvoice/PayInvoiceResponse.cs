using VSA.Contracts.Requests;

namespace GoodApp.Modules.Billing.Slices.PayInvoice;

public sealed record PayInvoiceResponse(bool Paid) : IResponse;

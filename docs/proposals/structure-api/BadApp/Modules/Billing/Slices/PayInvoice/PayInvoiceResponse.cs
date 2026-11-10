using VSA.Contracts.Requests;

namespace BadApp.Modules.Billing.Slices.PayInvoice;

public sealed record PayInvoiceResponse(bool Paid) : IResponse;

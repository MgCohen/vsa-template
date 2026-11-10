using System.Collections.Concurrent;
using BadApp.Modules.Billing.Domain;
using BadApp.Modules.Billing.Domain.Shared;

namespace BadApp.Modules.Billing.Infrastructure;

public sealed class InvoiceStore
{
    internal ConcurrentDictionary<InvoiceId, Invoice> Invoices { get; } = new();
}
